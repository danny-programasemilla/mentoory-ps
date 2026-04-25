using System.Text.RegularExpressions;
using FluentAssertions;
using MediatR;
using Mentoory.Access.Application.Commands.AdminEnrollUser;
using Mentoory.Access.Application.Commands.AssignRole;
using Mentoory.Access.Application.Commands.LoginUser;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Domain.Aggregates.SystemConfiguration;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.Interfaces;
using Mentoory.Shared.Application.Queries.Audit;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Shared.Infrastructure.Persistence.Audit;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Fixtures;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IntegrationTestBase(MentooryWebApplicationFactory factory)
    {
        Factory = factory;
    }

    protected MentooryWebApplicationFactory Factory { get; }

    public async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
        await SeedSystemConfigurationAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected IServiceScope CreateScope() => Factory.Services.CreateScope();

    protected async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request);
    }

    protected async Task<TResponse> SendWithTenantAsync<TResponse>(IRequest<TResponse> request, long? incubatorId)
    {
        using var scope = CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        ((Mentoory.Shared.Infrastructure.Services.TenantContextService)tenantContext).CurrentIncubatorId = incubatorId;
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request);
    }

    protected async Task<Result> RegisterUserAsync(
        string email = "test@example.com",
        string country = "CO",
        string nationalId = "123456789",
        string firstName = "Test",
        string lastName = "User",
        string password = "SecureP@ss123!")
    {
        return await SendAsync(new RegisterUserCommand(email, country, nationalId, firstName, lastName, password));
    }

    protected async Task<(Result RegisterResult, long UserId)> RegisterAndActivateUserAsync(
        string email = "test@example.com",
        string country = "CO",
        string nationalId = "123456789",
        string firstName = "Test",
        string lastName = "User",
        string password = "SecureP@ss123!")
    {
        var registerResult = await RegisterUserAsync(email, country, nationalId, firstName, lastName, password);

        if (registerResult.IsFailure)
        {
            return (registerResult, 0);
        }

        // Activate user directly via DbContext (email verification token flow
        // is not fully wired for integration tests)
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await dbContext.Users.FirstAsync(u => u.Email.NormalizedValue == normalizedEmail);
        user.Activate(DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        return (registerResult, user.Id);
    }

    protected async Task<(Result<LoginUserResult> LoginResult, long UserId)> RegisterActivateAndLoginAsync(
        string email = "test@example.com",
        string country = "CO",
        string nationalId = "123456789",
        string firstName = "Test",
        string lastName = "User",
        string password = "SecureP@ss123!")
    {
        var (registerResult, userId) = await RegisterAndActivateUserAsync(email, country, nationalId, firstName, lastName, password);

        if (registerResult.IsFailure)
        {
            return (Result<LoginUserResult>.Failure(ResultErrorCodes.GenericError, ("Setup", "Registration failed")), 0);
        }

        var loginResult = await SendAsync(new LoginUserCommand(email, password, "127.0.0.1", "TestAgent"));
        return (loginResult, userId);
    }

    /// <summary>
    /// Returns an HttpClient that has performed cookie-based login against `/Access/Login`
    /// AND auto-applied a single IncubatorAdmin context via `/Context/Select`, so the
    /// returned client's auth cookie carries the `ActiveRole` + `ActiveIncubatorId`
    /// claims that downstream `[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]`
    /// checks rely on. Self-seeds the admin user (Respawn truncates seed data
    /// before every test, so the helper must reseed). AllowAutoRedirect=false so
    /// the caller can assert on the 302 directly.
    /// </summary>
    protected async Task<HttpClient> CreateAuthenticatedAdminClientAsync(
        string email = "auto-admin@test.mentoory.com",
        string password = "AutoAdminTest123!@#")
    {
        await EnsureSeedAdminAsync(email, password);

        // BaseAddress = https://localhost so the CookieContainer accepts the Secure auth
        // cookie that ASP.NET Core Identity emits — over plain http the cookie would
        // be silently dropped from subsequent requests, breaking auth.
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        var loginPage = await client.GetAsync("/Access/Login");
        loginPage.EnsureSuccessStatusCode();
        var loginPageHtml = await loginPage.Content.ReadAsStringAsync();
        var antiforgeryToken = AntiforgeryHelper.ExtractToken(loginPageHtml);

        var loginForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
        });

        var loginResponse = await client.PostAsync("/Access/Login", loginForm);

        if (loginResponse.StatusCode != System.Net.HttpStatusCode.Redirect &&
            loginResponse.StatusCode != System.Net.HttpStatusCode.Found)
        {
            var body = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Admin login failed for '{email}' (expected 302; got {(int)loginResponse.StatusCode}). " +
                $"First 300 chars of response: {body[..Math.Min(300, body.Length)]}");
        }

        // ContextController.Select auto-applies the (single) role assignment
        // and updates the auth cookie with the ActiveRole + ActiveIncubatorId
        // claims. Without this hop, [Authorize(Roles=...)] checks fail.
        var selectLocation = loginResponse.Headers.Location?.ToString() ?? "/Context/Select";
        var selectResponse = await client.GetAsync(selectLocation);
        if (selectResponse.StatusCode != System.Net.HttpStatusCode.Redirect &&
            selectResponse.StatusCode != System.Net.HttpStatusCode.Found &&
            selectResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            var body = await selectResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Context selection failed for '{email}' (status {(int)selectResponse.StatusCode}). " +
                $"First 300 chars of response: {body[..Math.Min(300, body.Length)]}");
        }

        return client;
    }

    /// <summary>
    /// Returns the most-recent <c>[audit].[AuditLog]</c> row matching the given event type
    /// (and optionally user email) and asserts it exists. Use this to confirm an audited
    /// command has been captured by the pipeline.
    /// </summary>
    protected async Task<AuditLogReadEntity> AssertAuditLoggedAsync(
        string expectedEventType,
        string? userEmail = null)
    {
        using var scope = CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AuditReadDbContext>();

        var query = ctx.AuditLogs.AsNoTracking().Where(r => r.EventType == expectedEventType);
        if (userEmail is not null)
        {
            query = query.Where(r => r.UserEmail == userEmail);
        }

        var row = await query.OrderByDescending(r => r.OccurredAtUtc).FirstOrDefaultAsync();
        row.Should().NotBeNull($"expected an audit row with EventType '{expectedEventType}'");
        return row!;
    }

    private async Task EnsureSeedAdminAsync(string email, string password)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        long userId;

        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
            var existing = await dbContext.Users
                .Where(u => u.Email.NormalizedValue == normalizedEmail)
                .Select(u => new { u.Id, IsActive = u.EmailVerifiedAtUtc != null })
                .FirstOrDefaultAsync();

            if (existing is not null && existing.IsActive)
            {
                var hasActiveRole = await dbContext.RoleAssignments
                    .AnyAsync(r => r.UserId == existing.Id
                                   && r.Role == Roles.IncubatorAdmin
                                   && r.IsActive);
                if (hasActiveRole)
                {
                    return;
                }

                userId = existing.Id;
            }
            else
            {
                userId = 0;
            }
        }

        if (userId == 0)
        {
            // Use AdminEnrollUserCommand rather than RegisterUserCommand: the public
            // path masks every outcome to Success() (016 enumeration-oracle hardening),
            // so a failing registration is invisible. The admin path returns real
            // errors so a misconfigured fixture surfaces immediately.
            var enrollResult = await SendAsync(new AdminEnrollUserCommand(
                email,
                "CO",
                $"AUTO-{Guid.NewGuid():N}",
                "Auto",
                "Admin",
                password));
            if (enrollResult.IsFailure)
            {
                var detail = enrollResult.ErrorMessages is null
                    ? "(no error details)"
                    : string.Join(",", enrollResult.ErrorMessages.Select(e => $"{e.Context}:{e.Message}"));
                throw new InvalidOperationException($"AdminEnrollUserCommand failed for seed admin '{email}': {detail}");
            }

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
            var user = await dbContext.Users.FirstAsync(u => u.Email.NormalizedValue == normalizedEmail);
            if (user.EmailVerifiedAtUtc is null)
            {
                user.Activate(DateTime.UtcNow);
                await dbContext.SaveChangesAsync();
            }

            userId = user.Id;
        }

        long incubatorId;
        using (var scope = CreateScope())
        {
            var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            var existing = await tenantDb.Incubators
                .Select(i => new { i.Id })
                .FirstOrDefaultAsync();
            if (existing is not null)
            {
                incubatorId = existing.Id;
            }
            else
            {
                // Insert directly via DbContext rather than dispatching CreateIncubatorCommand.
                // The mediator path goes through TransactionBehavior, which commits in a fresh
                // scope's DbContext — but the entity instance lives on that scope's tracker,
                // and the new scope we'd use to read back the generated identity column may
                // not see the row before the commit fully drains. Direct insert here is
                // self-contained: one scope, one Add, one SaveChangesAsync, identity populated.
                var inc = Incubator.Create(
                    "Test Auto Incubator",
                    "Auto-seeded for integration tests",
                    DateTime.UtcNow);
                tenantDb.Incubators.Add(inc);
                await tenantDb.SaveChangesAsync();
                incubatorId = inc.Id;
            }
        }

        var roleResult = await SendAsync(new AssignRoleCommand(userId, incubatorId, null, Roles.IncubatorAdmin));
        if (roleResult.IsFailure)
        {
            var detail = roleResult.ErrorMessages is null
                ? "(no error details)"
                : string.Join(",", roleResult.ErrorMessages.Select(e => $"{e.Context}:{e.Message}"));
            throw new InvalidOperationException($"AssignRoleCommand failed: {detail}");
        }
    }

    private async Task SeedSystemConfigurationAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var utcNow = DateTime.UtcNow;

        var configs = new[]
        {
            ("EmailVerificationTokenExpiryHours", "24", "Integer"),
            ("PasswordResetTokenExpiryHours", "1", "Integer"),
            ("InvitationTokenExpiryHours", "72", "Integer"),
            ("MaxFailedLoginAttempts", "5", "Integer"),
            ("LockoutDurationMinutes", "15", "Integer"),
            ("SessionTimeoutHours", "8", "Integer"),
            ("PasswordHistoryDepth", "5", "Integer"),
        };

        foreach (var (key, value, dataType) in configs)
        {
            if (!await dbContext.SystemConfigurations.AnyAsync(c => c.Key == key))
            {
                dbContext.SystemConfigurations.Add(
                    SystemConfiguration.Create(key, value, dataType, null, utcNow));
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
