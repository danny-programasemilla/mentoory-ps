using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.SubmitDiagnosticResponse;
using Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;
using Mentoory.Diagnostic.Domain.Aggregates.StageFormAssignment;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Handlers;

public class SubmitDiagnosticResponseHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AssignmentExternalId = Guid.NewGuid();

    private readonly Mock<IStageFormAssignmentRepository> _assignmentRepo = new();
    private readonly Mock<IDiagnosticResponseRepository> _responseRepo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IIntegrationEventService> _eventService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SubmitDiagnosticResponseHandler _handler;

    public SubmitDiagnosticResponseHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _responseRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);

        _handler = new SubmitDiagnosticResponseHandler(
            _assignmentRepo.Object,
            _responseRepo.Object,
            _timeProvider.Object,
            _eventService.Object,
            Mock.Of<ILogger<SubmitDiagnosticResponseHandler>>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateAndPersist()
    {
        var assignment = StageFormAssignment.Create(
            projectId: 10, incubatorId: 1, projectStageId: 1, projectFormId: 1,
            selectedQuestionIds: new List<long> { 1, 2 }, utcNow: UtcNow);

        _assignmentRepo
            .Setup(r => r.GetByExternalIdAsync(AssignmentExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var command = new SubmitDiagnosticResponseCommand(
            AssignmentExternalId, 10, 1, 100,
            new List<ResponseItem>
            {
                new(1, "Answer 1", null, null),
                new(2, null, 4.5m, null),
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _responseRepo.Verify(r => r.Add(It.Is<DiagnosticResponse>(d =>
            d.IsCompleted && d.QuestionResponses.Count == 2)), Times.Once);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventService.Verify(e => e.PublishAsync(
            It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentAssignment_ShouldReturnFailure()
    {
        _assignmentRepo
            .Setup(r => r.GetByExternalIdAsync(AssignmentExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageFormAssignment?)null);

        var command = new SubmitDiagnosticResponseCommand(
            AssignmentExternalId, 10, 1, 100,
            new List<ResponseItem> { new(1, "A", null, null) });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        _responseRepo.Verify(r => r.Add(It.IsAny<DiagnosticResponse>()), Times.Never);
    }
}
