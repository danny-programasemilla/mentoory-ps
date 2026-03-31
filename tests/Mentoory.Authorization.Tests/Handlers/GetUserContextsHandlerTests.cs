using FluentAssertions;
using Mentoory.Authorization.Application.Queries.GetUserContexts;
using Mentoory.Authorization.Domain.Aggregates.RoleAssignment;
using Mentoory.Authorization.Domain.Repositories;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Shared.Domain.SeedWork;
using Moq;
using Xunit;

namespace Mentoory.Authorization.Tests.Handlers;

public class GetUserContextsHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IRoleAssignmentRepository> _repo = new();
    private readonly GetUserContextsHandler _handler;

    public GetUserContextsHandlerTests()
    {
        _repo.Setup(r => r.UnitOfWork).Returns(Mock.Of<IUnitOfWork>());
        _handler = new GetUserContextsHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_WithMultipleRoles_ReturnsAll()
    {
        var assignments = new List<RoleAssignment>
        {
            RoleAssignment.Create(1, 10, null, Roles.IncubatorAdmin, UtcNow),
            RoleAssignment.Create(1, 10, 20, Roles.Mentor, UtcNow),
        };

        _repo.Setup(r => r.GetActiveByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);

        var query = new GetUserContextsQuery(1);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Should().Contain(c => c.Role == Roles.IncubatorAdmin);
        result.Value.Should().Contain(c => c.Role == Roles.Mentor);
    }

    [Fact]
    public async Task Handle_WithNoRoles_ReturnsEmptyList()
    {
        _repo.Setup(r => r.GetActiveByUserIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleAssignment>());

        var query = new GetUserContextsQuery(999);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsContextFieldsCorrectly()
    {
        var assignment = RoleAssignment.Create(42, 100, 200, Roles.ProjectCoordinator, UtcNow);
        _repo.Setup(r => r.GetActiveByUserIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleAssignment> { assignment });

        var query = new GetUserContextsQuery(42);
        var result = await _handler.Handle(query, CancellationToken.None);

        var context = result.Value!.Single();
        context.UserId.Should().Be(42);
        context.IncubatorId.Should().Be(100);
        context.ProjectId.Should().Be(200);
        context.Role.Should().Be(Roles.ProjectCoordinator);
        context.RoleAssignmentExternalId.Should().Be(assignment.ExternalId);
        context.IncubatorName.Should().BeNull();
        context.ProjectName.Should().BeNull();
    }
}
