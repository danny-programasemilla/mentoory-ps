using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.SyncFromTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Handlers;

public class SyncFromTemplateHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid FormExternalId = Guid.NewGuid();

    private readonly Mock<IProjectFormRepository> _formRepo = new();
    private readonly Mock<IFormTemplateRepository> _templateRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SyncFromTemplateHandler _handler;

    public SyncFromTemplateHandlerTests()
    {
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _formRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);

        _handler = new SyncFromTemplateHandler(
            _formRepo.Object,
            _templateRepo.Object,
            Mock.Of<ILogger<SyncFromTemplateHandler>>());
    }

    [Fact]
    public async Task Handle_WithValidSync_ShouldSyncAndPersist()
    {
        var template = CreateTemplate();
        var form = ProjectForm.CloneFromTemplate(template, 10, 1, UtcNow);
        form.EnablePartialSync();

        // Add new question to template
        template.AddQuestion(3, "Pregunta 3", QuestionType.Numeric, 3, null, false);

        _formRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(FormExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);
        _templateRepo
            .Setup(r => r.GetByIdWithQuestionsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var command = new SyncFromTemplateCommand(FormExternalId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        form.Questions.Should().HaveCount(3);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentForm_ShouldReturnFailure()
    {
        _formRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(FormExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectForm?)null);

        var result = await _handler.Handle(new SyncFromTemplateCommand(FormExternalId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }

    [Fact]
    public async Task Handle_WhenFormHasNoSourceTemplate_ShouldReturnFailure()
    {
        var form = ProjectForm.Create("Custom Form", 10, 1, UtcNow);
        _formRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(FormExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var result = await _handler.Handle(new SyncFromTemplateCommand(FormExternalId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSourceTemplateNotFound_ShouldReturnFailure()
    {
        var template = CreateTemplate();
        var form = ProjectForm.CloneFromTemplate(template, 10, 1, UtcNow);

        _formRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(FormExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);
        _templateRepo
            .Setup(r => r.GetByIdWithQuestionsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FormTemplate?)null);

        var result = await _handler.Handle(new SyncFromTemplateCommand(FormExternalId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    private static FormTemplate CreateTemplate()
    {
        var template = FormTemplate.Create("Template", null, null, UtcNow);
        template.AddQuestion(1, "Q1", QuestionType.SingleSelect, 1, null, false);
        template.AddQuestion(2, "Q2", QuestionType.Text, 2, null, true);
        return template;
    }
}
