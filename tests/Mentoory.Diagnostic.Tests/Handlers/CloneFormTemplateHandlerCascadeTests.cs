using System.Reflection;
using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Diagnostic.Tests.Handlers;

/// <summary>
/// Tests the FR-K20 through FR-K23 cascade behavior of <see cref="CloneFormTemplateHandler"/>:
/// when a form template declares a <c>DefaultKnowledgeStructureTemplateExternalId</c>, the handler
/// must auto-provision (or reuse) a project knowledge structure and rewrite every cloned question's
/// <c>TopicId</c> from template-topic ids to project-topic ids.
/// </summary>
public class CloneFormTemplateHandlerCascadeTests
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
    private readonly Mock<IUnitOfWork> _ksStructureUow = new();
    private readonly CloneFormTemplateHandler _handler;

    public CloneFormTemplateHandlerCascadeTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _projectFormUow.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _ksStructureUow.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _projectFormRepo.Setup(r => r.UnitOfWork).Returns(_projectFormUow.Object);
        _ksStructureRepo.Setup(r => r.UnitOfWork).Returns(_ksStructureUow.Object);

        _handler = new CloneFormTemplateHandler(
            _formTemplateRepo.Object,
            _projectFormRepo.Object,
            _ksTemplateRepo.Object,
            _ksStructureRepo.Object,
            _timeProvider.Object,
            Mock.Of<ILogger<CloneFormTemplateHandler>>());
    }

    [Fact]
    public async Task Cascade_CreatesNewKnowledgeStructure_WhenNoneExists()
    {
        // Arrange: knowledge template with one topic, form template bound to it, no existing clone.
        var (ksTemplate, templateTopicId) = CreateKsTemplateWithSingleTopic();
        var formTemplate = CreateFormTemplateBoundTo(ksTemplate.ExternalId, templateTopicId);

        _formTemplateRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(formTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formTemplate);

        _ksTemplateRepo
            .Setup(r => r.GetByExternalIdAsync(ksTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ksTemplate);
        _ksTemplateRepo
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(ksTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ksTemplate);

        _ksStructureRepo
            .Setup(r => r.GetByProjectAndSourceTemplateIdAsync(ProjectId, ksTemplate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KS?)null);

        ProjectForm? capturedForm = null;
        _projectFormRepo.Setup(r => r.Add(It.IsAny<ProjectForm>()))
            .Callback<ProjectForm>(pf => capturedForm = pf)
            .Returns((ProjectForm pf) => pf);

        KS? addedStructure = null;
        _ksStructureRepo.Setup(r => r.Add(It.IsAny<KS>()))
            .Callback<KS>(ks =>
            {
                addedStructure = ks;
                // Simulate EF persistence assigning ids to the graph so the rewrite map can resolve.
                AssignDescendingIds(ks);
            })
            .Returns((KS ks) => ks);

        // Act
        var result = await _handler.Handle(
            new CloneFormTemplateCommand(formTemplate.ExternalId, ProjectId, IncubatorId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        addedStructure.Should().NotBeNull("cascade must Add a new KnowledgeStructure when none exists");
        _ksStructureRepo.Verify(r => r.Add(It.IsAny<KS>()), Times.Once);
        _ksStructureUow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _projectFormUow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        capturedForm.Should().NotBeNull();
        var clonedQuestion = capturedForm!.Questions.Single();
        var projectTopic = addedStructure!.Modules.Single().Topics.Single();
        clonedQuestion.TopicId.Should().Be(projectTopic.Id,
            "the rewrite map must redirect the template-topic id to the project-topic id");
    }

    [Fact]
    public async Task Cascade_ReusesExistingKnowledgeStructure_WhenOnePresent()
    {
        // Arrange: knowledge template + an already-cloned project structure.
        var (ksTemplate, templateTopicId) = CreateKsTemplateWithSingleTopic();
        var formTemplate = CreateFormTemplateBoundTo(ksTemplate.ExternalId, templateTopicId);

        var existingStructure = KS.CloneFromTemplate(ksTemplate, ProjectId, IncubatorId, UtcNow);
        AssignDescendingIds(existingStructure);

        _formTemplateRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(formTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formTemplate);

        _ksTemplateRepo
            .Setup(r => r.GetByExternalIdAsync(ksTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ksTemplate);
        _ksTemplateRepo
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(ksTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ksTemplate);

        _ksStructureRepo
            .Setup(r => r.GetByProjectAndSourceTemplateIdAsync(ProjectId, ksTemplate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStructure);

        ProjectForm? capturedForm = null;
        _projectFormRepo.Setup(r => r.Add(It.IsAny<ProjectForm>()))
            .Callback<ProjectForm>(pf => capturedForm = pf)
            .Returns((ProjectForm pf) => pf);

        // Act
        var result = await _handler.Handle(
            new CloneFormTemplateCommand(formTemplate.ExternalId, ProjectId, IncubatorId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _ksStructureRepo.Verify(r => r.Add(It.IsAny<KS>()), Times.Never,
            "reuse path must NOT Add a fresh structure");
        _ksStructureUow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never,
            "reuse path has no Knowledge writes to persist");
        _projectFormUow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        capturedForm.Should().NotBeNull();
        var clonedQuestion = capturedForm!.Questions.Single();
        var projectTopic = existingStructure.Modules.Single().Topics.Single();
        clonedQuestion.TopicId.Should().Be(projectTopic.Id);
    }

    [Fact]
    public async Task Cascade_RewritesTopicIds_InClonedQuestions()
    {
        // Arrange: knowledge template with TWO topics (template ids 100 and 200); existing project
        // structure whose topics got persisted-ids 9100 and 9200. Form template has questions
        // referencing TemplateTopicId 100 and 200 — these must become 9100 and 9200.
        var ksTemplate = KnowledgeStructureTemplate.Create("KT", null, UtcNow);
        SetEntityId(ksTemplate, 50L);
        var kmodule = ksTemplate.AddModule("M", null, 1);
        SetEntityId(kmodule, 60L);
        var topicA = ksTemplate.AddTopic(kmodule.ExternalId, "Topic A", null, 1);
        SetEntityId(topicA, 100L);
        var topicB = ksTemplate.AddTopic(kmodule.ExternalId, "Topic B", null, 2);
        SetEntityId(topicB, 200L);

        var existingStructure = KS.CloneFromTemplate(ksTemplate, ProjectId, IncubatorId, UtcNow);
        SetEntityId(existingStructure, 9000L);
        var projectModule = existingStructure.Modules.Single();
        SetEntityId(projectModule, 9050L);
        // The cloned topics are created in the same SortOrder sequence as their templates.
        var projectTopics = projectModule.Topics.OrderBy(t => t.SortOrder).ToList();
        SetEntityId(projectTopics[0], 9100L); // mirrors topicA (SourceTemplateTopicExternalId == topicA.ExternalId)
        SetEntityId(projectTopics[1], 9200L); // mirrors topicB

        var formTemplate = FormTemplate.Create("Form", null, null, UtcNow);
        formTemplate.SetDefaultKnowledgeStructureTemplate(ksTemplate.ExternalId);
        formTemplate.AddQuestion(100L, "QA", QuestionType.SingleSelect, StageApplicability.Both, 1, null, false);
        formTemplate.AddQuestion(200L, "QB", QuestionType.Text, StageApplicability.Both, 2, null, false);

        _formTemplateRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(formTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formTemplate);
        _ksTemplateRepo
            .Setup(r => r.GetByExternalIdAsync(ksTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ksTemplate);
        _ksTemplateRepo
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(ksTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ksTemplate);
        _ksStructureRepo
            .Setup(r => r.GetByProjectAndSourceTemplateIdAsync(ProjectId, ksTemplate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStructure);

        ProjectForm? capturedForm = null;
        _projectFormRepo.Setup(r => r.Add(It.IsAny<ProjectForm>()))
            .Callback<ProjectForm>(pf => capturedForm = pf)
            .Returns((ProjectForm pf) => pf);

        // Act
        var result = await _handler.Handle(
            new CloneFormTemplateCommand(formTemplate.ExternalId, ProjectId, IncubatorId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedForm.Should().NotBeNull();
        var questions = capturedForm!.Questions.OrderBy(q => q.SortOrder).ToList();
        questions.Should().HaveCount(2);
        questions[0].TopicId.Should().Be(9100L, "the QA question's template-topic 100 must be rewritten to project-topic 9100");
        questions[1].TopicId.Should().Be(9200L, "the QB question's template-topic 200 must be rewritten to project-topic 9200");
    }

    [Fact]
    public async Task Cascade_ThrowsAndReturnsFailure_WhenTemplateQuestionReferencesUnmappedTopic()
    {
        // Arrange: form template question references template-topic-id 999 (which the KS template
        // does NOT contain). The rewrite map cannot resolve it — factory throws, handler converts
        // to a Spanish Failure (FR-K23).
        var (ksTemplate, _) = CreateKsTemplateWithSingleTopic();
        var formTemplate = FormTemplate.Create("Form", null, null, UtcNow);
        formTemplate.SetDefaultKnowledgeStructureTemplate(ksTemplate.ExternalId);
        formTemplate.AddQuestion(999L, "Orphan", QuestionType.Text, StageApplicability.Both, 1, null, false);

        _formTemplateRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(formTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formTemplate);
        _ksTemplateRepo
            .Setup(r => r.GetByExternalIdAsync(ksTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ksTemplate);
        _ksTemplateRepo
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(ksTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ksTemplate);

        var existingStructure = KS.CloneFromTemplate(ksTemplate, ProjectId, IncubatorId, UtcNow);
        AssignDescendingIds(existingStructure);
        _ksStructureRepo
            .Setup(r => r.GetByProjectAndSourceTemplateIdAsync(ProjectId, ksTemplate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStructure);

        // Act
        var result = await _handler.Handle(
            new CloneFormTemplateCommand(formTemplate.ExternalId, ProjectId, IncubatorId),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages!.Should().Contain(m =>
            m.Context == "Cascada"
            && m.Message.Contains("No se puede clonar", StringComparison.OrdinalIgnoreCase));
        _projectFormRepo.Verify(r => r.Add(It.IsAny<ProjectForm>()), Times.Never,
            "a rewrite failure must NOT persist a partial ProjectForm");
    }

    [Fact]
    public async Task NoCascade_WhenFormTemplateHasNoDefaultBinding()
    {
        // Arrange: form template WITHOUT DefaultKnowledgeStructureTemplateExternalId.
        const long literalTopicId = 42L;
        var formTemplate = FormTemplate.Create("Form", null, null, UtcNow);
        formTemplate.AddQuestion(literalTopicId, "Q", QuestionType.Text, StageApplicability.Both, 1, null, false);

        _formTemplateRepo
            .Setup(r => r.GetByExternalIdWithQuestionsAsync(formTemplate.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formTemplate);

        ProjectForm? capturedForm = null;
        _projectFormRepo.Setup(r => r.Add(It.IsAny<ProjectForm>()))
            .Callback<ProjectForm>(pf => capturedForm = pf)
            .Returns((ProjectForm pf) => pf);

        // Act
        var result = await _handler.Handle(
            new CloneFormTemplateCommand(formTemplate.ExternalId, ProjectId, IncubatorId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedForm.Should().NotBeNull();
        capturedForm!.Questions.Single().TopicId.Should().Be(literalTopicId,
            "without a binding the template-topic id is preserved as-is");

        // No knowledge-module calls should have occurred.
        _ksTemplateRepo.VerifyNoOtherCalls();
        _ksStructureRepo.Verify(r => r.Add(It.IsAny<KS>()), Times.Never);
        _ksStructureRepo.Verify(r => r.GetByProjectAndSourceTemplateIdAsync(
            It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
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

    /// <summary>
    /// Walks a knowledge structure graph and assigns synthetic Ids (mirroring what EF would do
    /// on <c>SaveChanges</c>). Only called after creation so the rewrite map has project-topic
    /// ids to point at.
    /// </summary>
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

    /// <summary>
    /// Uses reflection to set the <c>Entity.Id</c> protected setter for test fixtures.
    /// </summary>
    private static void SetEntityId(object entity, long id)
    {
        var prop = entity.GetType().GetProperty(
            "Id",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        prop!.SetValue(entity, id);
    }
}
