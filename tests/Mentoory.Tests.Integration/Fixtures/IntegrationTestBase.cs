using MediatR;
using Mentoory.Access.Application.Commands.LoginUser;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Domain.Aggregates.SystemConfiguration;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Knowledge.Infrastructure.Persistence;
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
        await SeedKnowledgeTopicsAsync();
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
    /// Re-creates the <c>knowledge.Topics</c> rows with fixed Ids 1-5 that pre-PR-14 Diagnostic
    /// integration tests (Questions with literal TopicId values) rely on. Respawn wipes the
    /// knowledge schema between tests, so the DACPAC post-deploy seed is not sufficient on its
    /// own. Mirrors the production seed in <c>004.SeedTestData.sql § 3.5</c>.
    /// </summary>
    private async Task SeedKnowledgeTopicsAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();

        // Guid literals are static compile-time constants, no injection surface.
        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM [knowledge].[Topics] WHERE [Id] BETWEEN 1 AND 5)
            BEGIN
                DECLARE @StructureExternalId UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999999901' AS UNIQUEIDENTIFIER);
                DECLARE @ModuleExternalId UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999999902' AS UNIQUEIDENTIFIER);
                DECLARE @TemplateExternalId UNIQUEIDENTIFIER = CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER);

                IF NOT EXISTS (SELECT 1 FROM [knowledge].[KnowledgeStructureTemplates] WHERE [ExternalId] = @TemplateExternalId)
                BEGIN
                    INSERT INTO [knowledge].[KnowledgeStructureTemplates] ([ExternalId], [Name], [Description], [IsArchived], [Version], [CreatedAtUtc])
                    VALUES (@TemplateExternalId, N'Emprendimiento Básico', N'Plantilla de ejemplo.', 0, 1, SYSUTCDATETIME());
                END

                DECLARE @TemplateId BIGINT = (SELECT [Id] FROM [knowledge].[KnowledgeStructureTemplates] WHERE [ExternalId] = @TemplateExternalId);

                IF NOT EXISTS (SELECT 1 FROM [knowledge].[KnowledgeStructures] WHERE [ExternalId] = @StructureExternalId)
                BEGIN
                    -- ProjectId = 999999 is an out-of-band sentinel that test ProjectIds (typically
                    -- 10-99) never use, so the seed KS isn't counted by cascade reuse queries.
                    INSERT INTO [knowledge].[KnowledgeStructures] ([ExternalId], [ProjectId], [IncubatorId], [Name], [Description], [SourceTemplateId], [SourceTemplateVersion], [SyncMode], [CreatedAtUtc])
                    VALUES (@StructureExternalId, 999999, 999999, N'Test KS (FK parent for seeded Topics 1-5)', NULL, @TemplateId, 1, 0, SYSUTCDATETIME());
                END

                DECLARE @StructureId BIGINT = (SELECT [Id] FROM [knowledge].[KnowledgeStructures] WHERE [ExternalId] = @StructureExternalId);

                IF NOT EXISTS (SELECT 1 FROM [knowledge].[Modules] WHERE [ExternalId] = @ModuleExternalId)
                BEGIN
                    INSERT INTO [knowledge].[Modules] ([ExternalId], [KnowledgeStructureId], [SourceTemplateModuleExternalId], [Name], [Description], [SortOrder])
                    VALUES (@ModuleExternalId, @StructureId, NULL, N'Diagnostic', NULL, 1);
                END

                DECLARE @ModuleId BIGINT = (SELECT [Id] FROM [knowledge].[Modules] WHERE [ExternalId] = @ModuleExternalId);

                SET IDENTITY_INSERT [knowledge].[Topics] ON;

                INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [Name], [SortOrder]) VALUES (1, NEWID(), @ModuleId, N'Topic 1', 1);
                INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [Name], [SortOrder]) VALUES (2, NEWID(), @ModuleId, N'Topic 2', 2);
                INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [Name], [SortOrder]) VALUES (3, NEWID(), @ModuleId, N'Topic 3', 3);
                INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [Name], [SortOrder]) VALUES (4, NEWID(), @ModuleId, N'Topic 4', 4);
                INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [Name], [SortOrder]) VALUES (5, NEWID(), @ModuleId, N'Topic 5', 5);

                SET IDENTITY_INSERT [knowledge].[Topics] OFF;
            END";

        await dbContext.Database.ExecuteSqlRawAsync(sql);
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
