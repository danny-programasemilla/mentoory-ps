using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.CustomizeProjectForm;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Handlers;

public class CustomizeProjectFormHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid FormExternalId = Guid.NewGuid();

    private readonly Mock<IProjectFormRepository> _formRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CustomizeProjectFormHandler _handler;

    public CustomizeProjectFormHandlerTests()
    {
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _formRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);

        _handler = new CustomizeProjectFormHandler(
            _formRepo.Object,
            Mock.Of<ILogger<CustomizeProjectFormHandler>>());
    }

    [Fact]
    public async Task Handle_AddQuestion_ShouldAddToForm()
    {
        var form = ProjectForm.Create("Form", 10, 1, UtcNow);
        _formRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(FormExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var command = new CustomizeProjectFormCommand(
            FormExternalId,
            CustomizeAction.AddQuestion,
            new QuestionData(1, "New Q", QuestionType.Text, StageApplicability.Both, 1, null, false),
            null,
            null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        form.Questions.Should().HaveCount(1);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AddQuestion_WithNullQuestionData_ShouldReturnFailure()
    {
        var form = ProjectForm.Create("Form", 10, 1, UtcNow);
        _formRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(FormExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var command = new CustomizeProjectFormCommand(
            FormExternalId, CustomizeAction.AddQuestion, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentForm_ShouldReturnFailure()
    {
        _formRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(FormExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectForm?)null);

        var command = new CustomizeProjectFormCommand(
            FormExternalId, CustomizeAction.AddQuestion,
            new QuestionData(1, "Q", QuestionType.Text, StageApplicability.Both, 1, null, false),
            null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }
}
