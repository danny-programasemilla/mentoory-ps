using System.Reflection;
using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Diagnostic.Tests.Handlers;

/// <summary>
/// Tests the post-amendment (spec 016 Phase 9) behavior of <see cref="CloneFormTemplateHandler"/>:
/// the project's <c>KnowledgeStructure</c> is materialized at project creation, so this handler
/// only verifies compatibility between the form template's <c>DefaultKnowledgeStructureTemplateExternalId</c>
/// and the project's KS source template, then rewrites <c>Question.TopicId</c> values against the
/// project's existing topics. No KS writes occur here.
/// </summary>
public class CloneFormTemplateHandlerCompatibilityTests
{
    private const long ProjectId = 10L;
    private const long IncubatorId = 1L;
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IFormTemplateRepository> _formTemplateRepo = new();
    private readonly Mock<IProjectFormRepository> _projectFormRepo = new();
    private readonly Mock<IKnowledgeStructureTemplateRepository> _ksTemplateRepo = new();
    private readonly Mock<IKnowledgeStructureRepository> _ksStructureRepo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _projectFormUow = new();
    private readonly CloneFormTemplateHandler _handler;

    public CloneFormTemplateHandlerCompatibilityTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _projectFormUow.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _projectFormRepo.Setup(r => r.UnitOfWork).Returns(_projectFormUow.Object);

        _handler = new CloneFormTemplateHandler(
            _formTemplateRepo.Object,
            _projectFormRepo.Object,
            _ksTemplateRepo.Object,
            _ksStructureRepo.Object,
            _timeProvider.Object,
            Mock.Of<ILogger<CloneFormTemplateHandler>>());
    }

    [Fact]
    public async Task Compatible_FormBoundToProjectsKs_RewritesTopicIdsAndSavesForm()
    {
        var (ksTemplate, templateTopicId) = CreateKsTemplateWithSingleTopic();
        var formTemplate = CreateFormTemplateBoundTo(ksTemplate.ExternalId, templateTopicId);
        var projectStructure = CreateProjectStructureFrom(ksTemplate);

        SetupFormLoad(formTemplate);
        SetupProjectKsLookup(projectStructure, ksTemplate);

        ProjectForm? capturedForm = null;
        _projectFormRepo
            .Setup(r => r.Add(It.IsAny<ProjectForm>()))
            .Callback<ProjectForm>(pf => capturedForm = pf)
            .Returns((ProjectForm pf) => pf);

        var result = await _handler.Handle(
            new CloneFormTemplateCommand(formTemplate.ExternalId, ProjectId, IncubatorId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedForm.Should().NotBeNull();

        var clonedQuestion = capturedForm!.Questions.Single();
        var projectTopic = projectStructure.Modules.Single().Topics.Single();
        clonedQuestion.TopicId.Should().Be(projectTopic.Id,
            "rewrite map redirects the template-topic id to the project-topic id");

        _ksStructureRepo.Verify(r => r.Add(It.IsAny<KS>()), Times.Never,
            "Phase 9 handler never writes to Knowledge — KS is materialized at project creation");
        _projectFormUow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Incompatible_FormBoundToDifferentKs_ReturnsFailureWithSpanishMessage()
    {
        var (projectKsTemplate, _) = CreateKsTemplateWithSingleTopic();
        var (otherKsTemplate, otherTopicId) = CreateKsTemplateWithSingleTopic();
        SetEntityId(otherKsTemplate, 950L);

        var formTemplate = CreateFormTemplateBoundTo(otherKsTemplate.ExternalId, otherTopicId);
        var projectStructure = CreateProjectStructureFrom(projectKsTemplate);

        SetupFormLoad(formTemplate);
        SetupProjectKsLookup(projectStructure, projectKsTemplate);

        var result = await _handler.Handle(
            new CloneFormTemplateCommand(formTemplate.ExternalId, ProjectId, IncubatorId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages!.Should().Contain(m =>
            m.Context == "Cascada"
            && m.Message.Contains("estructura de conocimiento diferente", StringComparison.OrdinalIgnoreCase));
        _projectFormRepo.Verify(r => r.Add(It.IsAny<ProjectForm>()), Times.Never);
    }

    [Fact]
    public async Task Unbound_FormWithoutDefaultKs_PassesThroughWithoutRewrite()
    {
        const long literalTopicId = 42L;
        var formTemplate = FormTemplate.Create("Form", null, null, UtcNow);
        formTemplate.AddQuestion(literalTopicId, "Q", QuestionType.Text, StageApplicability.Both, 1, null, false);

        SetupFormLoad(formTemplate);

        ProjectForm? capturedForm = null;
        _projectFormRepo
            .Setup(r => r.Add(It.IsAny<ProjectForm>()))
            .Callback<ProjectForm>(pf => capturedForm = pf)
            .Returns((ProjectForm pf) => pf);

        var result = await _handler.Handle(
            new CloneFormTemplateCommand(formTemplate.ExternalId, ProjectId, IncubatorId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedForm!.Questions.Single().TopicId.Should().Be(literalTopicId,
            "without a DefaultKS binding the template-topic id is preserved as-is");

        _ksStructureRepo.VerifyNoOtherCalls();
        _ksTemplateRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProjectWithoutKs_ReturnsDefensiveFailure()
    {
        var (ksTemplate, templateTopicId) = CreateKsTemplateWithSingleTopic();
        var formTemplate = CreateFormTemplateBoundTo(ksTemplate.ExternalId, templateTopicId);

        SetupFormLoad(formTemplate);
        _ksStructureRepo
            .Setup(r => r.GetByProjectIdAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KS?)null);

        var result = await _handler.Handle(
            new CloneFormTemplateCommand(formTemplate.ExternalId, ProjectId, IncubatorId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages!.Should().Contain(m => m.Context == "KnowledgeStructure");
        _projectFormRepo.Verify(r => r.Add(It.IsAny<ProjectForm>()), Times.Never);
    }

    [Fact]
    public async Task UnmappedTopic_ReturnsFailureFromFactoryRewrite()
    {
        var (ksTemplate, _) = CreateKsTemplateWithSingleTopic();
        var projectStructure = CreateProjectStructureFrom(ksTemplate);

        var formTemplate = FormTemplate.Create("Form", null, null, UtcNow);
        formTemplate.SetDefaultKnowledgeStructureTemplate(ksTemplate.ExternalId);
        formTemplate.AddQuestion(999L, "Orphan", QuestionType.Text, StageApplicability.Both, 1, null, false);

        SetupFormLoad(formTemplate);
        SetupProjectKsLookup(projectStructure, ksTemplate);

        var result = await _handler.Handle(
            new CloneFormTemplateCommand(formTemplate.ExternalId, ProjectId, IncubatorId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages!.Should().Contain(m =>
            m.Context == "Cascada"
            && m.Message.Contains("No se puede clonar", StringComparison.OrdinalIgnoreCase));
        _projectFormRepo.Verify(r => r.Add(It.IsAny<ProjectForm>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------
    private static (KnowledgeStructureTemplate KsTemplate, long TemplateTopicId) CreateKsTemplateWithSingleTopic()
    {
        var ksTemplate = KnowledgeStructureTemplate.Create("KT", null, UtcNow);
        SetEntityId(ksTemplate, 50L);
        var kmodule = ksTemplate.AddModule("M", null, 1);
        SetEntityId(kmodule, 60L);
        var topic = ksTemplate.AddTopic(kmodule.ExternalId, "Topic", null, 1);
        SetEntityId(topic, 100L);
        return (ksTemplate, 100L);
    }

    private static FormTemplate CreateFormTemplateBoundTo(Guid ksTemplateExternalId, long templateTopicId)
    {
        var formTemplate = FormTemplate.Create("Form", null, null, UtcNow);
        formTemplate.SetDefaultKnowledgeStructureTemplate(ksTemplateExternalId);
        formTemplate.AddQuestion(templateTopicId, "Q", QuestionType.Text, StageApplicability.Both, 1, null, false);
        return formTemplate;
    }

    private static KS CreateProjectStructureFrom(KnowledgeStructureTemplate ksTemplate)
    {
        var projectStructure = KS.CloneFromTemplate(ksTemplate, ProjectId, IncubatorId, UtcNow);
        AssignDescendingIds(projectStructure);
        return projectStructure;
    }

    private static void AssignDescendingIds(KS structure)
    {
        SetEntityId(structure, 9000L);
        long next = 9001L;
        foreach (var m in structure.Modules)
        {
            SetEntityId(m, next++);
            foreach (var t in m.Topics)
            {
                SetEntityId(t, next++);
                foreach (var s in t.Subjects)
                {
                    SetEntityId(s, next++);
                    foreach (var r in s.Resources)
                    {
                        SetEntityId(r, next++);
                    }
                }
            }
        }
    }

    private static void SetEntityId(object entity, long id)
    {
        var prop = entity.GetType().GetProperty(
            "Id",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        prop!.SetValue(entity, id);
    }

    private void SetupFormLoad(FormTemplate formTemplate) =>
        _formTemplateRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(formTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formTemplate);

    private void SetupProjectKsLookup(KS projectStructure, KnowledgeStructureTemplate sourceTemplate)
    {
        _ksStructureRepo
            .Setup(r => r.GetByProjectIdAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectStructure);
        _ksTemplateRepo
            .Setup(r => r.GetByIdAsync(sourceTemplate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceTemplate);
        _ksTemplateRepo
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(sourceTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceTemplate);
    }
}
