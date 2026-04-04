using FluentAssertions;
using Mentoory.Access.Application.Queries.ListIncubatorMembers;
using Mentoory.Access.Domain.Aggregates.RoleAssignment;
using Mentoory.Access.Domain.ReadModels;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Tests.Infrastructure;
using Mentoory.Shared.Application.DataTables;
using Moq;
using Xunit;

namespace Mentoory.Access.Tests.Handlers;

public class ListIncubatorMembersHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserProfileRepository> _userProfileRepo = new();
    private readonly Mock<IRoleAssignmentRepository> _roleAssignmentRepo = new();
    private readonly ListIncubatorMembersHandler _handler;

    public ListIncubatorMembersHandlerTests()
    {
        _handler = new ListIncubatorMembersHandler(_userProfileRepo.Object, _roleAssignmentRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsFilteredMembers()
    {
        var profiles = new List<UserProfile>
        {
            UserProfile.Create(1, Guid.NewGuid(), "user1@test.com", "Juan", "Pérez", "Active", UtcNow, UtcNow),
            UserProfile.Create(2, Guid.NewGuid(), "user2@test.com", "María", "García", "Active", UtcNow, UtcNow),
            UserProfile.Create(3, Guid.NewGuid(), "user3@test.com", "Carlos", "López", "Active", UtcNow, UtcNow),
        };

        var roleAssignments = new List<RoleAssignment>
        {
            RoleAssignment.Create(1, 10, null, "IncubatorAdmin", UtcNow),
            RoleAssignment.Create(2, 10, null, "Entrepreneur", UtcNow),
        };

        _userProfileRepo.Setup(r => r.Query())
            .Returns(profiles.AsAsyncQueryable());
        _roleAssignmentRepo.Setup(r => r.Query())
            .Returns(roleAssignments.AsAsyncQueryable());

        var request = new DataTableRequest(1, 0, 10, null, "asc", null, null);
        var query = new ListIncubatorMembersQuery(request, 10);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RecordsTotal.Should().Be(2);
        result.Value.Data.Should().HaveCount(2);
        result.Value.Data.Should().Contain(d => d.Email == "user1@test.com");
        result.Value.Data.Should().Contain(d => d.Email == "user2@test.com");
        result.Value.Data.Should().NotContain(d => d.Email == "user3@test.com");
    }

    [Fact]
    public async Task Handle_WithSearch_FiltersResults()
    {
        var profiles = new List<UserProfile>
        {
            UserProfile.Create(1, Guid.NewGuid(), "juan@test.com", "Juan", "Pérez", "Active", UtcNow, UtcNow),
            UserProfile.Create(2, Guid.NewGuid(), "maria@test.com", "María", "García", "Active", UtcNow, UtcNow),
        };

        var roleAssignments = new List<RoleAssignment>
        {
            RoleAssignment.Create(1, 10, null, "IncubatorAdmin", UtcNow),
            RoleAssignment.Create(2, 10, null, "Entrepreneur", UtcNow),
        };

        _userProfileRepo.Setup(r => r.Query())
            .Returns(profiles.AsAsyncQueryable());
        _roleAssignmentRepo.Setup(r => r.Query())
            .Returns(roleAssignments.AsAsyncQueryable());

        var request = new DataTableRequest(1, 0, 10, null, "asc", "Juan", null);
        var query = new ListIncubatorMembersQuery(request, 10);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RecordsTotal.Should().Be(2);
        result.Value.RecordsFiltered.Should().Be(1);
        result.Value.Data.Should().HaveCount(1);
        result.Value.Data.First().FirstName.Should().Be("Juan");
    }

    [Fact]
    public async Task Handle_WithEmptyIncubator_ReturnsEmptyResult()
    {
        var profiles = new List<UserProfile>
        {
            UserProfile.Create(1, Guid.NewGuid(), "user1@test.com", "Juan", "Pérez", "Active", UtcNow, UtcNow),
        };

        var roleAssignments = new List<RoleAssignment>();

        _userProfileRepo.Setup(r => r.Query())
            .Returns(profiles.AsAsyncQueryable());
        _roleAssignmentRepo.Setup(r => r.Query())
            .Returns(roleAssignments.AsAsyncQueryable());

        var request = new DataTableRequest(1, 0, 10, null, "asc", null, null);
        var query = new ListIncubatorMembersQuery(request, 10);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RecordsTotal.Should().Be(0);
        result.Value.Data.Should().BeEmpty();
    }
}
