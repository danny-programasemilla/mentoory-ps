using MediatR;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Application.Commands.AdvanceProjectStage;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mentoory.Tests.E2E.Infrastructure.Lifecycle;

public sealed class LifecycleFixtures
{
    public const string ProjectNamePrefix = "e2e-lifecycle-";
    public const string IncubatorNamePrefix = "e2e-lifecycle-inc-";

    private const string IncubatorAName = IncubatorNamePrefix + "a";
    private const string IncubatorBName = IncubatorNamePrefix + "b";
    private const string GlobalAdminNormalizedEmail = "ADMIN@MENTOORY.COM";

    private readonly PlaywrightFixture _host;

    public LifecycleFixtures(PlaywrightFixture host)
    {
        _host = host;
    }

    public async Task ResetStateAsync(CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_host.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM [tenant].[Projects]   WHERE [Name] LIKE @ProjectPrefix + '%';
            DELETE FROM [tenant].[Incubators] WHERE [Name] LIKE @IncubatorPrefix + '%';";
        command.Parameters.AddWithValue("@ProjectPrefix", ProjectNamePrefix);
        command.Parameters.AddWithValue("@IncubatorPrefix", IncubatorNamePrefix);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<(Guid IncubatorA, Guid IncubatorB)> EnsureTwoIncubatorsAsync(CancellationToken ct = default)
    {
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var time = scope.ServiceProvider.GetRequiredService<ITimeProvider>();

        var existing = await db.Incubators
            .Where(i => i.Name == IncubatorAName || i.Name == IncubatorBName)
            .ToListAsync(ct);

        var incA = existing.FirstOrDefault(i => i.Name == IncubatorAName);
        var incB = existing.FirstOrDefault(i => i.Name == IncubatorBName);

        if (incA is null)
        {
            incA = Incubator.Create(IncubatorAName, "E2E lifecycle test incubator A", time.UtcNow);
            db.Incubators.Add(incA);
        }

        if (incB is null)
        {
            incB = Incubator.Create(IncubatorBName, "E2E lifecycle test incubator B", time.UtcNow);
            db.Incubators.Add(incB);
        }

        await db.SaveChangesAsync(ct);
        return (incA.ExternalId, incB.ExternalId);
    }

    public async Task<Guid> CreateProjectAsync(
        Guid incubatorExternalId,
        string name,
        StageType targetStage = StageType.Registration,
        bool active = true,
        CancellationToken ct = default)
    {
        if (!name.StartsWith(ProjectNamePrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Project name must start with '{ProjectNamePrefix}' so ResetStateAsync can clean it up.",
                nameof(name));
        }

        Guid projectExternalId;
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            var time = scope.ServiceProvider.GetRequiredService<ITimeProvider>();

            var incubator = await db.Incubators.FirstAsync(i => i.ExternalId == incubatorExternalId, ct);
            var project = Project.Create(incubator.Id, name, description: null, time.UtcNow);
            db.Projects.Add(project);
            await db.SaveChangesAsync(ct);
            projectExternalId = project.ExternalId;
        }

        if (targetStage > StageType.Registration)
        {
            var adminUserId = await GetGlobalAdminUserIdAsync(ct);

            using var scope = _host.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var advanceCount = (int)targetStage - (int)StageType.Registration;
            for (var i = 0; i < advanceCount; i++)
            {
                var command = new AdvanceProjectStageCommand(
                    projectExternalId,
                    ActingUserId: adminUserId,
                    ActingUserIncubatorId: 0,
                    ActingUserIsGlobalAdmin: true);

                var result = await mediator.Send(command, ct);
                if (result.IsFailure)
                {
                    throw new InvalidOperationException(
                        $"Failed to advance seeded project '{name}' toward {targetStage}: {result.ErrorCode}");
                }
            }
        }

        if (!active)
        {
            await SetProjectActiveAsync(projectExternalId, isActive: false, ct);
        }

        return projectExternalId;
    }

    public Task DeactivateProjectAsync(Guid projectExternalId, CancellationToken ct = default)
        => SetProjectActiveAsync(projectExternalId, isActive: false, ct);

    private async Task SetProjectActiveAsync(Guid projectExternalId, bool isActive, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_host.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE [tenant].[Projects] SET [IsActive] = @IsActive WHERE [ExternalId] = @ExternalId";
        command.Parameters.AddWithValue("@IsActive", isActive ? 1 : 0);
        command.Parameters.AddWithValue("@ExternalId", projectExternalId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<long> GetGlobalAdminUserIdAsync(CancellationToken ct)
    {
        await using var connection = new SqlConnection(_host.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TOP 1 [Id] FROM [access].[Users] WHERE [NormalizedEmail] = @Email";
        command.Parameters.AddWithValue("@Email", GlobalAdminNormalizedEmail);
        var scalar = await command.ExecuteScalarAsync(ct);
        if (scalar is null or DBNull)
        {
            throw new InvalidOperationException(
                $"GlobalAdmin user with NormalizedEmail '{GlobalAdminNormalizedEmail}' not found. " +
                "Confirm 002.SeedGlobalAdmin.sql ran during DACPAC deployment.");
        }

        return Convert.ToInt64(scalar);
    }
}
