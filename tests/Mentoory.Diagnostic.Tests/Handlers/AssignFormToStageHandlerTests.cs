using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.AssignFormToStage;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Aggregates.StageFormAssignment;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Handlers;

public class AssignFormToStageHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IProjectFormRepository> _formRepo = new();
    private readonly Mock<IStageFormAssignmentRepository> _assignmentRepo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AssignFormToStageHandler _handler;

    public AssignFormToStageHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _assignmentRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _formRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _assignmentRepo.Setup(r => r.Add(It.IsAny<StageFormAssignment>()))
            .Returns((StageFormAssignment a) => a);

        _handler = new AssignFormToStageHandler(
            NullLogger<AssignFormToStageHandler>.Instance,
            _formRepo.Object,
            _assignmentRepo.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_WithValidForm_CreatesAssignmentWithAllQuestions()
    {
        var form = ProjectForm.Create("Test Form", 10, 1, UtcNow);
        form.AddQuestion(1, "Q1", QuestionType.Text, 1, null, false);
        form.AddQuestion(2, "Q2", QuestionType.SingleSelect, 2, null, false);

        _formRepo.Setup(r => r.GetByExternalIdWithQuestionsAsync(form.ExternalId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var command = new AssignFormToStageCommand(10, 1, 5, form.ExternalId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _assignmentRepo.Verify(r => r.Add(It.Is<StageFormAssignment>(
            a => a.AssignedQuestions.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentForm_ReturnsFailure()
    {
        _formRepo.Setup(r => r.GetByExternalIdWithQuestionsAsync(It.IsAny<Guid>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectForm?)null);

        var command = new AssignFormToStageCommand(10, 1, 5, Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _assignmentRepo.Verify(r => r.Add(It.IsAny<StageFormAssignment>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyForm_ReturnsFailure()
    {
        var form = ProjectForm.Create("Empty Form", 10, 1, UtcNow);

        _formRepo.Setup(r => r.GetByExternalIdWithQuestionsAsync(form.ExternalId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var command = new AssignFormToStageCommand(10, 1, 5, form.ExternalId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _assignmentRepo.Verify(r => r.Add(It.IsAny<StageFormAssignment>()), Times.Never);
    }
}
