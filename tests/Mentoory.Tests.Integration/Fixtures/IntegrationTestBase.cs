using MediatR;
using Mentoory.Access.Application.Commands.LoginUser;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Domain.Aggregates.SystemConfiguration;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.Interfaces;
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
