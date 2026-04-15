using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.CorrectAnswer;
using Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Handlers;

public class CorrectAnswerHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ResponseExternalId = Guid.NewGuid();

    private readonly Mock<IDiagnosticResponseRepository> _responseRepo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IIntegrationEventService> _eventService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CorrectAnswerHandler _handler;

    public CorrectAnswerHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _responseRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);

        _handler = new CorrectAnswerHandler(
            _responseRepo.Object,
            _timeProvider.Object,
            _eventService.Object,
            Mock.Of<ILogger<CorrectAnswerHandler>>());
    }

    [Fact]
    public async Task Handle_WithValidCorrection_ShouldUpdateAndPublishEvent()
    {
        var response = CreateCompletedResponse();
        var qrId = response.QuestionResponses.First().Id;

        _responseRepo
            .Setup(r => r.GetByExternalIdWithResponsesAsync(ResponseExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var command = new CorrectAnswerCommand(
            ResponseExternalId, qrId, "Corrected", null, null, 200, "Typo fix");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _responseRepo.Verify(r => r.Update(response), Times.Once);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventService.Verify(e => e.PublishAsync(
            It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentResponse_ShouldReturnFailure()
    {
        _responseRepo
            .Setup(r => r.GetByExternalIdWithResponsesAsync(ResponseExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiagnosticResponse?)null);

        var command = new CorrectAnswerCommand(
            ResponseExternalId, 1, "New", null, null, 200, "Fix");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }

    private static DiagnosticResponse CreateCompletedResponse()
    {
        var response = DiagnosticResponse.Create(1, 10, 1, 100, 1L, UtcNow);
        response.AddResponse(1, "Original", null, null, UtcNow);
        return response;
    }
}
