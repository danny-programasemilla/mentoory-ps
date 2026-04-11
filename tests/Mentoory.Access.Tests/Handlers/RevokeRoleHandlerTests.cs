using FluentAssertions;
using Mentoory.Access.Application.Commands.RevokeRole;
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

public class RevokeRoleHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IRoleAssignmentRepository> _repo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RevokeRoleHandler _handler;

    public RevokeRoleHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _repo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _handler = new RevokeRoleHandler(
            NullLogger<RevokeRoleHandler>.Instance,
            _repo.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_WithExistingAssignment_Revokes()
    {
        var assignment = RoleAssignment.Create(1, 10, null, Roles.Mentor, UtcNow);
        var externalId = assignment.ExternalId;

        _repo.Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var command = new RevokeRoleCommand(externalId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.IsActive.Should().BeFalse();
        _repo.Verify(r => r.Update(assignment), Times.Once);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentAssignment_ReturnsFailure()
    {
        var randomId = Guid.NewGuid();
        _repo.Setup(r => r.GetByExternalIdAsync(randomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleAssignment?)null);

        var command = new RevokeRoleCommand(randomId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }

    [Fact]
    public async Task Handle_WithAlreadyRevokedAssignment_ThrowsDomainException()
    {
        var assignment = RoleAssignment.Create(1, 10, null, Roles.Mentor, UtcNow);
        assignment.Revoke(UtcNow);
        var externalId = assignment.ExternalId;

        _repo.Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var command = new RevokeRoleCommand(externalId);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
