using FluentAssertions;
using Mentoory.Access.Application.Commands.AssignRole;
using Mentoory.Access.Domain.Aggregates.RoleAssignment;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Access.Tests.Handlers;

public class AssignRoleHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IRoleAssignmentRepository> _repo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AssignRoleHandler _handler;

    public AssignRoleHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _repo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repo.Setup(r => r.Add(It.IsAny<RoleAssignment>())).Returns((RoleAssignment ra) => ra);

        _handler = new AssignRoleHandler(
            NullLogger<AssignRoleHandler>.Instance,
            _repo.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesRoleAssignment()
    {
        _repo.Setup(r => r.GetActiveAssignmentAsync(1, 10, null, Roles.Mentor, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleAssignment?)null);

        var command = new AssignRoleCommand(1, 10, null, Roles.Mentor);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.Add(It.IsAny<RoleAssignment>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateAssignment_ReturnsFailure()
    {
        var existing = RoleAssignment.Create(1, 10, null, Roles.Mentor, UtcNow);
        _repo.Setup(r => r.GetActiveAssignmentAsync(1, 10, null, Roles.Mentor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new AssignRoleCommand(1, 10, null, Roles.Mentor);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _repo.Verify(r => r.Add(It.IsAny<RoleAssignment>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EntrepreneurLimit_ReturnsFailureWhenAlreadyAssigned()
    {
        _repo.Setup(r => r.GetActiveAssignmentAsync(1, 10, null, Roles.Entrepreneur, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleAssignment?)null);
        _repo.Setup(r => r.CountActiveEntrepreneurAssignmentsAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AssignRoleCommand(1, 10, null, Roles.Entrepreneur);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EntrepreneurFirstAssignment_Succeeds()
    {
        _repo.Setup(r => r.GetActiveAssignmentAsync(1, 10, null, Roles.Entrepreneur, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleAssignment?)null);
        _repo.Setup(r => r.CountActiveEntrepreneurAssignmentsAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new AssignRoleCommand(1, 10, null, Roles.Entrepreneur);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
