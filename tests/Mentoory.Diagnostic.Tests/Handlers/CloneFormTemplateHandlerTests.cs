using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Handlers;

public class CloneFormTemplateHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid TemplateExternalId = Guid.NewGuid();

    private readonly Mock<IFormTemplateRepository> _formTemplateRepo = new();
    private readonly Mock<IProjectFormRepository> _projectFormRepo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CloneFormTemplateHandler _handler;

    public CloneFormTemplateHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _projectFormRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);

        _handler = new CloneFormTemplateHandler(
            _formTemplateRepo.Object,
            _projectFormRepo.Object,
            _timeProvider.Object,
            Mock.Of<ILogger<CloneFormTemplateHandler>>());
    }

    [Fact]
    public async Task Handle_WithValidTemplate_ShouldCloneAndPersist()
    {
        var template = CreateTemplate();
        _formTemplateRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(TemplateExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var command = new CloneFormTemplateCommand(TemplateExternalId, 10, 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _projectFormRepo.Verify(r => r.Add(It.Is<ProjectForm>(f =>
            f.ProjectId == 10 && f.IncubatorId == 1 && f.Name == "Template")), Times.Once);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentTemplate_ShouldReturnFailure()
    {
        _formTemplateRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(TemplateExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FormTemplate?)null);

        var command = new CloneFormTemplateCommand(TemplateExternalId, 10, 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        _projectFormRepo.Verify(r => r.Add(It.IsAny<ProjectForm>()), Times.Never);
    }

    private static FormTemplate CreateTemplate()
    {
        var template = FormTemplate.Create("Template", "Desc", null, UtcNow);
        template.AddQuestion(1, "Q1", QuestionType.SingleSelect, StageApplicability.Both, 1, null, false);
        return template;
    }
}
