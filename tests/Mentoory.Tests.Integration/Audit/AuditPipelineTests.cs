using FluentAssertions;
using Mentoory.Access.Application.Commands.AssignRole;
using Mentoory.Access.Application.Commands.LoginUser;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Application.Commands.SetActiveContext;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Application.Commands.CreateIncubator;
using Mentoory.Tenant.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Audit;

/// <summary>
/// Integration coverage for the four Automatic-mode commands retrofitted in feature 016
/// (US1). Each retrofitted command produces exactly one <c>[audit].[AuditLog]</c> row via
/// the <c>AuditingBehavior</c> MediatR pipeline behavior.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class AuditPipelineTests : IntegrationTestBase
{
    public AuditPipelineTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task RegisterUser_Success_WritesAuditRow_WithEmailAndRedactedPassword()
    {
        var result = await SendAsync(new RegisterUserCommand(
            "register-audit@example.com", "CO", "111222333", "Register", "Audit", "SecureP@ss123!"));
        result.IsSuccess.Should().BeTrue();

        var row = await AssertAuditLoggedAsync(AuditEventTypes.UserRegistered);
        row.Action.Should().Be(nameof(RegisterUserCommand));
        row.Outcome.Should().Be("Success");
        row.UserEmail.Should().Be("register-audit@example.com");
        row.UserId.Should().BeNull("RegisterUser runs before the tenant context has a user id");
        row.Details.Should().NotBeNull();
        row.Details!.Should().Contain("***REDACTED***");
        row.Details.Should().NotContain("SecureP@ss123!");
    }

    [Fact]
    public async Task RegisterUser_DuplicateEmail_WritesFailureRow()
    {
        await SendAsync(new RegisterUserCommand(
            "dup-audit@example.com", "CO", "111222334", "Dup", "Audit", "SecureP@ss123!"));

        var dup = await SendAsync(new RegisterUserCommand(
            "dup-audit@example.com", "CO", "111222335", "Dup2", "Audit", "SecureP@ss123!"));
        dup.IsFailure.Should().BeTrue();

        var row = await AssertAuditLoggedAsync(AuditEventTypes.UserRegistered, "dup-audit@example.com");
        row.Outcome.Should().Be("Failure");
        row.ExceptionType.Should().BeNull("Result.Failure is not an exception");
    }

    [Fact]
    public async Task LoginUser_Success_WritesAuditRow_WithEmailAndRedactedPassword()
    {
        await RegisterAndActivateUserAsync(email: "login-audit@example.com", nationalId: "222333444");

        var result = await SendAsync(new LoginUserCommand(
            "login-audit@example.com", "SecureP@ss123!", "10.0.0.99", "Test/1.0"));
        result.IsSuccess.Should().BeTrue();

        var row = await AssertAuditLoggedAsync(AuditEventTypes.UserLoggedIn, "login-audit@example.com");
        row.Action.Should().Be(nameof(LoginUserCommand));
        row.Outcome.Should().Be("Success");
        row.UserEmail.Should().Be("login-audit@example.com");
        row.Details.Should().NotBeNull();
        row.Details!.Should().Contain("***REDACTED***");
        row.Details.Should().NotContain("SecureP@ss123!");
    }

    [Fact]
    public async Task LoginUser_InvalidPassword_WritesFailureRow()
    {
        await RegisterAndActivateUserAsync(email: "badlogin-audit@example.com", nationalId: "333444555");

        var result = await SendAsync(new LoginUserCommand(
            "badlogin-audit@example.com", "WrongPass!", "10.0.0.100", "Test/1.0"));
        result.IsFailure.Should().BeTrue();

        var row = await AssertAuditLoggedAsync(AuditEventTypes.UserLoggedIn, "badlogin-audit@example.com");
        row.Outcome.Should().Be("Failure");
        row.ExceptionType.Should().BeNull();
    }

    [Fact]
    public async Task AssignRole_Success_WritesAuditRow()
    {
        var (_, userId) = await RegisterAndActivateUserAsync(
            email: "assign-audit@example.com", nationalId: "444555666");

        var incubatorResult = await SendAsync(new CreateIncubatorCommand("Audit Incubator", null));
        var incubatorId = await GetIncubatorIdAsync(incubatorResult.Value!);

        var result = await SendAsync(new AssignRoleCommand(userId, incubatorId, null, Roles.IncubatorAdmin));
        result.IsSuccess.Should().BeTrue();

        var row = await AssertAuditLoggedAsync(AuditEventTypes.RoleAssigned);
        row.Action.Should().Be(nameof(AssignRoleCommand));
        row.Outcome.Should().Be("Success");
    }

    [Fact]
    public async Task AssignRole_DuplicateAssignment_WritesFailureRow()
    {
        var (_, userId) = await RegisterAndActivateUserAsync(
            email: "assign-dup@example.com", nationalId: "444555777");

        var incubatorResult = await SendAsync(new CreateIncubatorCommand("Reject Incubator", null));
        var incubatorId = await GetIncubatorIdAsync(incubatorResult.Value!);

        var first = await SendAsync(new AssignRoleCommand(userId, incubatorId, null, Roles.IncubatorAdmin));
        first.IsSuccess.Should().BeTrue();

        var dup = await SendAsync(new AssignRoleCommand(userId, incubatorId, null, Roles.IncubatorAdmin));
        dup.IsFailure.Should().BeTrue();

        var row = await AssertAuditLoggedAsync(AuditEventTypes.RoleAssigned);
        row.Outcome.Should().Be("Failure");
    }

    [Fact]
    public async Task SetActiveContext_Success_WritesAuditRow()
    {
        var (_, userId) = await RegisterAndActivateUserAsync(
            email: "ctx-audit@example.com", nationalId: "555666777");

        var incubatorResult = await SendAsync(new CreateIncubatorCommand("Ctx Incubator", null));
        var incubatorId = await GetIncubatorIdAsync(incubatorResult.Value!);
        await SendAsync(new AssignRoleCommand(userId, incubatorId, null, Roles.IncubatorAdmin));

        using var scope = CreateScope();
        var authCtx = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var assignment = await authCtx.RoleAssignments
            .FirstAsync(ra => ra.UserId == userId && ra.Role == Roles.IncubatorAdmin);

        var result = await SendAsync(new SetActiveContextCommand(userId, assignment.ExternalId));
        result.IsSuccess.Should().BeTrue();

        var row = await AssertAuditLoggedAsync(AuditEventTypes.ContextActivated);
        row.Action.Should().Be(nameof(SetActiveContextCommand));
        row.Outcome.Should().Be("Success");
    }

    [Fact]
    public async Task SetActiveContext_UnknownAssignment_WritesFailureRow()
    {
        var (_, userId) = await RegisterAndActivateUserAsync(
            email: "ctx-fail-audit@example.com", nationalId: "666777888");

        var result = await SendAsync(new SetActiveContextCommand(userId, Guid.NewGuid()));
        result.IsFailure.Should().BeTrue();

        var row = await AssertAuditLoggedAsync(AuditEventTypes.ContextActivated);
        row.Outcome.Should().Be("Failure");
    }

    private async Task<long> GetIncubatorIdAsync(Guid externalId)
    {
        using var scope = CreateScope();
        var tenantCtx = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var incubator = await tenantCtx.Incubators.FirstAsync(i => i.ExternalId == externalId);
        return incubator.Id;
    }
}
