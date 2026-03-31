using FluentAssertions;
using Mentoory.Authorization.Application.Commands.AssignRole;
using Mentoory.Authorization.Application.Commands.SetActiveContext;
using Mentoory.Authorization.Application.Queries.GetUserContexts;
using Mentoory.Authorization.Infrastructure.Persistence;
using Mentoory.Shared.Application;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Application.Commands.CreateIncubator;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Authorization;

[Collection(IntegrationTestCollection.Name)]
public class ContextSelectionTests : IntegrationTestBase
{
    public ContextSelectionTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetUserContexts_ReturnsAllActiveRoleAssignments()
    {
        // Arrange
        var (_, userId) = await RegisterAndActivateUserAsync(
            email: "contexts@example.com", nationalId: "900100100");

        // Create an incubator to get its internal ID
        var incubatorResult = await SendAsync(new CreateIncubatorCommand("Test Incubator", "For context tests"));
        incubatorResult.IsSuccess.Should().BeTrue();

        // Get incubator ID from DB
        var incubatorId = await GetIncubatorIdAsync(incubatorResult.Value!);

        // Assign multiple roles
        var adminAssign = await SendAsync(new AssignRoleCommand(userId, incubatorId, null, Roles.IncubatorAdmin));
        adminAssign.IsSuccess.Should().BeTrue();

        var mentorAssign = await SendAsync(new AssignRoleCommand(userId, incubatorId, null, Roles.Mentor));
        mentorAssign.IsSuccess.Should().BeTrue();

        // Act
        var result = await SendAsync(new GetUserContextsQuery(userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Should().Contain(c => c.Role == Roles.IncubatorAdmin);
        result.Value.Should().Contain(c => c.Role == Roles.Mentor);
    }

    [Fact]
    public async Task SetActiveContext_WithValidAssignment_ReturnsUserContext()
    {
        // Arrange
        var (_, userId) = await RegisterAndActivateUserAsync(
            email: "setcontext@example.com", nationalId: "900200200");

        var incubatorResult = await SendAsync(new CreateIncubatorCommand("Context Incubator", null));
        incubatorResult.IsSuccess.Should().BeTrue();
        var incubatorId = await GetIncubatorIdAsync(incubatorResult.Value!);

        await SendAsync(new AssignRoleCommand(userId, incubatorId, null, Roles.IncubatorAdmin));

        // Get the role assignment external ID
        using var scope = CreateScope();
        var authContext = scope.ServiceProvider.GetRequiredService<AuthorizationDbContext>();
        var roleAssignment = await authContext.RoleAssignments
            .FirstAsync(ra => ra.UserId == userId && ra.Role == Roles.IncubatorAdmin);

        // Act
        var result = await SendAsync(new SetActiveContextCommand(userId, roleAssignment.ExternalId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Role.Should().Be(Roles.IncubatorAdmin);
        result.Value.IncubatorId.Should().Be(incubatorId);
        result.Value.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task SetActiveContext_WithNonOwnedAssignment_Fails()
    {
        // Arrange — create two users
        var (_, userId1) = await RegisterAndActivateUserAsync(
            email: "user1@example.com", nationalId: "900300300");
        var (_, userId2) = await RegisterAndActivateUserAsync(
            email: "user2@example.com", nationalId: "900400400");

        var incubatorResult = await SendAsync(new CreateIncubatorCommand("Owned Incubator", null));
        var incubatorId = await GetIncubatorIdAsync(incubatorResult.Value!);

        // Assign role to user1
        await SendAsync(new AssignRoleCommand(userId1, incubatorId, null, Roles.IncubatorAdmin));

        // Get user1's role assignment
        using var scope = CreateScope();
        var authContext = scope.ServiceProvider.GetRequiredService<AuthorizationDbContext>();
        var roleAssignment = await authContext.RoleAssignments
            .FirstAsync(ra => ra.UserId == userId1 && ra.Role == Roles.IncubatorAdmin);

        // Act — user2 tries to use user1's assignment
        var result = await SendAsync(new SetActiveContextCommand(userId2, roleAssignment.ExternalId));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }

    [Fact]
    public async Task SetActiveContext_WithNonExistentAssignment_Fails()
    {
        // Arrange
        var (_, userId) = await RegisterAndActivateUserAsync(
            email: "noassign@example.com", nationalId: "900500500");

        // Act
        var result = await SendAsync(new SetActiveContextCommand(userId, Guid.NewGuid()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }

    private async Task<long> GetIncubatorIdAsync(Guid externalId)
    {
        using var scope = CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<Mentoory.Tenant.Infrastructure.Persistence.TenantDbContext>();
        var incubator = await tenantContext.Incubators.FirstAsync(i => i.ExternalId == externalId);
        return incubator.Id;
    }
}
