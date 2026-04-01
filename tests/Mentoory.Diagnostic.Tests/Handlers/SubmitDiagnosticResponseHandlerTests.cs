using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.SubmitDiagnosticResponse;
using Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
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
    private static readonly Guid FormExternalId = Guid.NewGuid();

    private readonly Mock<IProjectFormRepository> _formRepo = new();
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
            _formRepo.Object,
            _responseRepo.Object,
            _timeProvider.Object,
            _eventService.Object,
            Mock.Of<ILogger<SubmitDiagnosticResponseHandler>>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateAndPersist()
    {
        var form = ProjectForm.Create("Test Form", 10, 1, UtcNow);
        _formRepo
            .Setup(r => r.GetByExternalIdAsync(FormExternalId, 10L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var command = new SubmitDiagnosticResponseCommand(
            FormExternalId, 10, 1, 100, EvaluationStage.Initial,
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
    public async Task Handle_WithNonExistentForm_ShouldReturnFailure()
    {
        _formRepo
            .Setup(r => r.GetByExternalIdAsync(FormExternalId, 10L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectForm?)null);

        var command = new SubmitDiagnosticResponseCommand(
            FormExternalId, 10, 1, 100, EvaluationStage.Initial,
            new List<ResponseItem> { new(1, "A", null, null) });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        _responseRepo.Verify(r => r.Add(It.IsAny<DiagnosticResponse>()), Times.Never);
    }
}
