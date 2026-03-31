using FluentAssertions;
using Mentoory.Authorization.Application.Queries.CheckPermission;
using Mentoory.Authorization.Domain.Aggregates.RoleAssignment;
using Mentoory.Authorization.Domain.Enums;
using Mentoory.Authorization.Domain.Repositories;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Shared.Domain.SeedWork;
using Moq;
using Xunit;

namespace Mentoory.Authorization.Tests.Handlers;

public class CheckPermissionHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IRoleAssignmentRepository> _repo = new();
    private readonly CheckPermissionHandler _handler;

    public CheckPermissionHandlerTests()
    {
        _repo.Setup(r => r.UnitOfWork).Returns(Mock.Of<IUnitOfWork>());
        _handler = new CheckPermissionHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_GlobalAdmin_HasAllPermissions()
    {
        var assignment = RoleAssignment.Create(1, 10, null, Roles.GlobalAdmin, UtcNow);
        _repo.Setup(r => r.GetActiveAssignmentAsync(1, 10, null, Roles.GlobalAdmin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var query = new CheckPermissionQuery(1, 10, null, Roles.GlobalAdmin, nameof(Permission.ManageProjects));
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Entrepreneur_CannotManageProjects()
    {
        var assignment = RoleAssignment.Create(1, 10, null, Roles.Entrepreneur, UtcNow);
        _repo.Setup(r => r.GetActiveAssignmentAsync(1, 10, null, Roles.Entrepreneur, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var query = new CheckPermissionQuery(1, 10, null, Roles.Entrepreneur, nameof(Permission.ManageProjects));
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Entrepreneur_CanCompleteDiagnostic()
    {
        var assignment = RoleAssignment.Create(1, 10, null, Roles.Entrepreneur, UtcNow);
        _repo.Setup(r => r.GetActiveAssignmentAsync(1, 10, null, Roles.Entrepreneur, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var query = new CheckPermissionQuery(1, 10, null, Roles.Entrepreneur, nameof(Permission.CompleteDiagnostic));
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoAssignment_ReturnsFalse()
    {
        _repo.Setup(r => r.GetActiveAssignmentAsync(1, 10, null, Roles.Mentor, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleAssignment?)null);

        var query = new CheckPermissionQuery(1, 10, null, Roles.Mentor, nameof(Permission.ManageSessions));
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_InvalidPermissionName_ReturnsFalse()
    {
        var assignment = RoleAssignment.Create(1, 10, null, Roles.GlobalAdmin, UtcNow);
        _repo.Setup(r => r.GetActiveAssignmentAsync(1, 10, null, Roles.GlobalAdmin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var query = new CheckPermissionQuery(1, 10, null, Roles.GlobalAdmin, "NonExistentPermission");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Mentor_HasManageSessions()
    {
        var assignment = RoleAssignment.Create(1, 10, 20, Roles.Mentor, UtcNow);
        _repo.Setup(r => r.GetActiveAssignmentAsync(1, 10, 20L, Roles.Mentor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var query = new CheckPermissionQuery(1, 10, 20, Roles.Mentor, nameof(Permission.ManageSessions));
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_IncubatorAdmin_HasManageIncubatorUsers()
    {
        var assignment = RoleAssignment.Create(1, 10, null, Roles.IncubatorAdmin, UtcNow);
        _repo.Setup(r => r.GetActiveAssignmentAsync(1, 10, null, Roles.IncubatorAdmin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var query = new CheckPermissionQuery(1, 10, null, Roles.IncubatorAdmin, nameof(Permission.ManageIncubatorUsers));
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}
