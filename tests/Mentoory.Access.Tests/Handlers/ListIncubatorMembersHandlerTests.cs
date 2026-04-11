using System.Reflection;
using FluentAssertions;
using Mentoory.Access.Application.Queries.ListIncubatorMembers;
using Mentoory.Access.Domain.Aggregates.RoleAssignment;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Tests.Infrastructure;
using Mentoory.Shared.Application.DataTables;
using Moq;
using Xunit;

namespace Mentoory.Access.Tests.Handlers;

public class ListIncubatorMembersHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRoleAssignmentRepository> _roleAssignmentRepo = new();
    private readonly ListIncubatorMembersHandler _handler;

    public ListIncubatorMembersHandlerTests()
    {
        _handler = new ListIncubatorMembersHandler(_userRepo.Object, _roleAssignmentRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsFilteredMembers()
    {
        var users = new List<User>
        {
            CreateUser(1, "user1@test.com", "CO", "100", "Juan", "Pérez"),
            CreateUser(2, "user2@test.com", "CO", "200", "María", "García"),
            CreateUser(3, "user3@test.com", "CO", "300", "Carlos", "López"),
        };

        var roleAssignments = new List<RoleAssignment>
        {
            RoleAssignment.Create(1, 10, null, "IncubatorAdmin", UtcNow),
            RoleAssignment.Create(2, 10, null, "Entrepreneur", UtcNow),
        };

        _userRepo.Setup(r => r.Query())
            .Returns(users.AsAsyncQueryable());
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
        var users = new List<User>
        {
            CreateUser(1, "juan@test.com", "CO", "100", "Juan", "Pérez"),
            CreateUser(2, "maria@test.com", "CO", "200", "María", "García"),
        };

        var roleAssignments = new List<RoleAssignment>
        {
            RoleAssignment.Create(1, 10, null, "IncubatorAdmin", UtcNow),
            RoleAssignment.Create(2, 10, null, "Entrepreneur", UtcNow),
        };

        _userRepo.Setup(r => r.Query())
            .Returns(users.AsAsyncQueryable());
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
        var users = new List<User>
        {
            CreateUser(1, "user1@test.com", "CO", "100", "Juan", "Pérez"),
        };

        var roleAssignments = new List<RoleAssignment>();

        _userRepo.Setup(r => r.Query())
            .Returns(users.AsAsyncQueryable());
        _roleAssignmentRepo.Setup(r => r.Query())
            .Returns(roleAssignments.AsAsyncQueryable());

        var request = new DataTableRequest(1, 0, 10, null, "asc", null, null);
        var query = new ListIncubatorMembersQuery(request, 10);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RecordsTotal.Should().Be(0);
        result.Value.Data.Should().BeEmpty();
    }

    private static User CreateUser(long id, string email, string country, string nationalId, string firstName, string lastName)
    {
        var user = User.Register(email, country, nationalId, firstName, lastName, "hash", UtcNow);
        typeof(User).GetProperty("Id")!.SetValue(user, id);
        return user;
    }
}
