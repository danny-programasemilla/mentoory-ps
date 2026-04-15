using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.RemoveFormFromStage;
using Mentoory.Diagnostic.Domain.Aggregates.StageFormAssignment;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Handlers;

public class RemoveFormFromStageHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IStageFormAssignmentRepository> _assignmentRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RemoveFormFromStageHandler _handler;

    public RemoveFormFromStageHandlerTests()
    {
        _assignmentRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);

        _handler = new RemoveFormFromStageHandler(
            NullLogger<RemoveFormFromStageHandler>.Instance,
            _assignmentRepo.Object);
    }

    [Fact]
    public async Task Handle_WithExistingAssignment_DeactivatesIt()
    {
        var assignment = StageFormAssignment.Create(10, 1, 5, 3, [100L], UtcNow);

        _assignmentRepo.Setup(r => r.GetByExternalIdAsync(assignment.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var command = new RemoveFormFromStageCommand(assignment.ExternalId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNonExistentAssignment_ReturnsFailure()
    {
        _assignmentRepo.Setup(r => r.GetByExternalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageFormAssignment?)null);

        var command = new RemoveFormFromStageCommand(Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAlreadyDeactivated_SucceedsIdempotently()
    {
        var assignment = StageFormAssignment.Create(10, 1, 5, 3, [100L], UtcNow);
        assignment.Deactivate();

        _assignmentRepo.Setup(r => r.GetByExternalIdAsync(assignment.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var command = new RemoveFormFromStageCommand(assignment.ExternalId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.IsActive.Should().BeFalse();
    }
}
