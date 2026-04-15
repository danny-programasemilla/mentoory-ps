using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.UpdateStageQuestionSelection;
using Mentoory.Diagnostic.Domain.Aggregates.StageFormAssignment;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Handlers;

public class UpdateStageQuestionSelectionHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IStageFormAssignmentRepository> _assignmentRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UpdateStageQuestionSelectionHandler _handler;

    public UpdateStageQuestionSelectionHandlerTests()
    {
        _assignmentRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);

        _handler = new UpdateStageQuestionSelectionHandler(
            NullLogger<UpdateStageQuestionSelectionHandler>.Instance,
            _assignmentRepo.Object);
    }

    [Fact]
    public async Task Handle_WithValidSelection_UpdatesQuestions()
    {
        var assignment = StageFormAssignment.Create(10, 1, 5, 3, [100L, 200L, 300L], UtcNow);

        _assignmentRepo.Setup(r => r.GetByExternalIdAsync(assignment.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var command = new UpdateStageQuestionSelectionCommand(assignment.ExternalId, [200L, 300L]);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.AssignedQuestions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNonExistentAssignment_ReturnsFailure()
    {
        _assignmentRepo.Setup(r => r.GetByExternalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageFormAssignment?)null);

        var command = new UpdateStageQuestionSelectionCommand(Guid.NewGuid(), [100L]);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithEmptySelection_ReturnsFailure()
    {
        var assignment = StageFormAssignment.Create(10, 1, 5, 3, [100L], UtcNow);

        _assignmentRepo.Setup(r => r.GetByExternalIdAsync(assignment.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var command = new UpdateStageQuestionSelectionCommand(assignment.ExternalId, []);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
