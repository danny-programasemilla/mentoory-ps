using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.CorrectAnswer;
using Mentoory.Diagnostic.Application.Commands.SubmitDiagnosticResponse;
using Mentoory.Diagnostic.Application.Queries.GetDiagnosticResponse;
using Mentoory.Diagnostic.Application.Queries.GetProjectForm;
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

/// <summary>
/// Verifies project-scoped isolation: handlers must reject access when
/// the requested entity does not belong to the specified project.
/// </summary>
public class ProjectIsolationTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    // T032: GetProjectFormHandler returns failure when form does not belong to requested project
    [Fact]
    public async Task GetProjectFormHandler_WhenFormNotInProject_ReturnsNull()
    {
        var formRepo = new Mock<IProjectFormRepository>();
        var formExternalId = Guid.NewGuid();
        const long wrongProjectId = 999;

        formRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(formExternalId, wrongProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectForm?)null);

        var handler = new GetProjectFormHandler(formRepo.Object);
        var query = new GetProjectFormQuery(formExternalId, wrongProjectId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetProjectFormHandler_WhenFormInProject_ReturnsDto()
    {
        var formRepo = new Mock<IProjectFormRepository>();
        var formExternalId = Guid.NewGuid();
        const long correctProjectId = 10;

        var form = ProjectForm.Create("Test", correctProjectId, 1, UtcNow);
        formRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(formExternalId, correctProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var handler = new GetProjectFormHandler(formRepo.Object);
        var query = new GetProjectFormQuery(formExternalId, correctProjectId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    // T033: GetDiagnosticResponseHandler returns failure when response does not belong to requested project
    [Fact]
    public async Task GetDiagnosticResponseHandler_WhenResponseNotInProject_ReturnsNull()
    {
        var responseRepo = new Mock<IDiagnosticResponseRepository>();
        var responseExternalId = Guid.NewGuid();
        const long wrongProjectId = 999;

        responseRepo
            .Setup(r => r.GetByExternalIdWithResponsesAsync(responseExternalId, wrongProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiagnosticResponse?)null);

        var handler = new GetDiagnosticResponseHandler(responseRepo.Object);
        var query = new GetDiagnosticResponseQuery(responseExternalId, wrongProjectId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // T034: SubmitDiagnosticResponseHandler rejects when form belongs to different project
    [Fact]
    public async Task SubmitDiagnosticResponseHandler_WhenFormNotInProject_ReturnsFailure()
    {
        var formRepo = new Mock<IProjectFormRepository>();
        var responseRepo = new Mock<IDiagnosticResponseRepository>();
        var timeProvider = new Mock<ITimeProvider>();
        var eventService = new Mock<IIntegrationEventService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        responseRepo.Setup(r => r.UnitOfWork).Returns(unitOfWork.Object);

        var formExternalId = Guid.NewGuid();
        const long wrongProjectId = 999;

        // Form does not belong to wrongProjectId, so project-scoped lookup returns null
        formRepo
            .Setup(r => r.GetByExternalIdAsync(formExternalId, wrongProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectForm?)null);

        var handler = new SubmitDiagnosticResponseHandler(
            formRepo.Object,
            responseRepo.Object,
            timeProvider.Object,
            eventService.Object,
            Mock.Of<ILogger<SubmitDiagnosticResponseHandler>>());

        var command = new SubmitDiagnosticResponseCommand(
            formExternalId, wrongProjectId, 1, 100, EvaluationStage.Initial, []);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }

    // T035: CorrectAnswerHandler rejects when response belongs to different project
    [Fact]
    public async Task CorrectAnswerHandler_WhenResponseNotInProject_ReturnsFailure()
    {
        var responseRepo = new Mock<IDiagnosticResponseRepository>();
        var timeProvider = new Mock<ITimeProvider>();
        var eventService = new Mock<IIntegrationEventService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        responseRepo.Setup(r => r.UnitOfWork).Returns(unitOfWork.Object);

        var responseExternalId = Guid.NewGuid();
        const long wrongProjectId = 999;

        responseRepo
            .Setup(r => r.GetByExternalIdWithResponsesAsync(responseExternalId, wrongProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiagnosticResponse?)null);

        var handler = new CorrectAnswerHandler(
            responseRepo.Object,
            timeProvider.Object,
            eventService.Object,
            Mock.Of<ILogger<CorrectAnswerHandler>>());

        var command = new CorrectAnswerCommand(
            responseExternalId, 1, "New", null, null, 200, "Fix", wrongProjectId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }
}
