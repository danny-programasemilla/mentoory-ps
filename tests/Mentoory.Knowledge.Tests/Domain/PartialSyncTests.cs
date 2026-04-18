using System.Reflection;
using FluentAssertions;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;
using Xunit;
using TemplateRoot = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate.KnowledgeStructureTemplate;

namespace Mentoory.Knowledge.Tests.Domain;

/// <summary>
/// Tests for <see cref="KnowledgeStructure.ApplyPartialSync"/> — the US5 domain method that
/// pulls template-side additions into an existing clone while leaving local items intact.
/// </summary>
public class PartialSyncTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ApplyPartialSync_AppendsNewTemplateTopics()
    {
        var template = BuildSimpleTemplate(templateId: 101);
        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);
        clone.SetSyncMode(SyncMode.PartialSync);

        // Template adds a second topic post-clone.
        var moduleExternalId = template.Modules.Single().ExternalId;
        template.AddTopic(moduleExternalId, "Tema 2", null, 2);

        var result = clone.ApplyPartialSync(template);

        result.TopicsAdded.Should().Be(1);
        var cloneModule = clone.Modules.Single();
        cloneModule.Topics.Should().HaveCount(2);

        var newCloneTopic = cloneModule.Topics.Single(t => t.Name == "Tema 2");
        newCloneTopic.SortOrder.Should().Be(cloneModule.Topics.Max(t => t.SortOrder));
        newCloneTopic.SourceTemplateTopicExternalId.Should().NotBeNull();
    }

    [Fact]
    public void ApplyPartialSync_AppendsNewSubjectUnderExistingTopic()
    {
        var template = BuildSimpleTemplate(templateId: 102);
        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);
        clone.SetSyncMode(SyncMode.PartialSync);

        var topicExternalId = template.Modules.Single().Topics.Single().ExternalId;
        template.AddSubject(topicExternalId, "Materia 2", null, 2);

        var result = clone.ApplyPartialSync(template);

        result.SubjectsAdded.Should().Be(1);
        var cloneTopic = clone.Modules.Single().Topics.Single();
        cloneTopic.Subjects.Should().HaveCount(2);

        var newSubject = cloneTopic.Subjects.Single(s => s.Name == "Materia 2");
        newSubject.SortOrder.Should().Be(cloneTopic.Subjects.Max(s => s.SortOrder));
        newSubject.SourceTemplateSubjectExternalId.Should().NotBeNull();
    }

    [Fact]
    public void ApplyPartialSync_DoesNotModifyRenamedClonedTopic()
    {
        var template = BuildSimpleTemplate(templateId: 103);
        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);
        clone.SetSyncMode(SyncMode.PartialSync);

        // Rename the cloned topic locally.
        var cloneTopic = clone.Modules.Single().Topics.Single();
        clone.UpdateTopic(cloneTopic.ExternalId, "Renombrado Localmente", "local");

        // Template also renames its own topic.
        var templateTopic = template.Modules.Single().Topics.Single();
        template.UpdateTopic(templateTopic.ExternalId, "Renombrado en Plantilla", null);

        clone.ApplyPartialSync(template);

        // The cloned topic keeps the local rename; partial sync only appends, never updates existing.
        clone.Modules.Single().Topics.Single().Name.Should().Be("Renombrado Localmente");
        clone.Modules.Single().Topics.Single().Description.Should().Be("local");
    }

    [Fact]
    public void ApplyPartialSync_DoesNotAffectLocallyAddedTopic()
    {
        var template = BuildSimpleTemplate(templateId: 104);
        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);
        clone.SetSyncMode(SyncMode.PartialSync);

        // Local-only addition (no source stamp).
        var moduleExternalId = clone.Modules.Single().ExternalId;
        clone.AddTopic(moduleExternalId, "Tema Local", null, 99);

        clone.ApplyPartialSync(template);

        var cloneModule = clone.Modules.Single();
        cloneModule.Topics.Should().Contain(t => t.Name == "Tema Local");
        cloneModule.Topics
            .Single(t => t.Name == "Tema Local")
            .SourceTemplateTopicExternalId.Should().BeNull();
    }

    [Fact]
    public void ApplyPartialSync_ThrowsInDisconnectedMode()
    {
        var template = BuildSimpleTemplate(templateId: 105);
        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);

        // Default SyncMode is Disconnected — do not switch to PartialSync.
        clone.SyncMode.Should().Be(SyncMode.Disconnected);

        var act = () => clone.ApplyPartialSync(template);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sincronización parcial*");
    }

    [Fact]
    public void ApplyPartialSync_ThrowsWhenTemplateIdMismatch()
    {
        var template = BuildSimpleTemplate(templateId: 106);
        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);
        clone.SetSyncMode(SyncMode.PartialSync);

        // Build a different template with a different Id.
        var otherTemplate = BuildSimpleTemplate(templateId: 999);

        var act = () => clone.ApplyPartialSync(otherTemplate);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no coincide con la plantilla de origen*");
    }

    [Fact]
    public void ApplyPartialSync_UpdatesSourceTemplateVersion()
    {
        var template = BuildSimpleTemplate(templateId: 107);
        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);
        clone.SetSyncMode(SyncMode.PartialSync);

        // Bump template version by adding a new topic.
        var moduleExternalId = template.Modules.Single().ExternalId;
        template.AddTopic(moduleExternalId, "Tema Nuevo", null, 2);
        var versionAfterAdd = template.Version;

        clone.ApplyPartialSync(template);

        clone.SourceTemplateVersion.Should().Be(versionAfterAdd);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds a fresh one-module/one-topic/one-subject/one-resource template and forces
    /// its Entity.Id to <paramref name="templateId"/> so we can satisfy
    /// <see cref="KnowledgeStructure.ApplyPartialSync"/>'s template-id match check
    /// (ids are otherwise assigned by EF at save time).
    /// </summary>
    private static TemplateRoot BuildSimpleTemplate(long templateId)
    {
        var template = TemplateRoot.Create("Plantilla", "desc", UtcNow);
        var module = template.AddModule("Módulo 1", null, 1);
        var topic = template.AddTopic(module.ExternalId, "Tema 1", null, 1);
        var subject = template.AddSubject(topic.ExternalId, "Materia 1", null, 1);
        template.AddResource(
            subject.ExternalId,
            "Recurso 1",
            null,
            "https://example.com/r",
            ResourceType.Link,
            1);

        SetEntityId(template, templateId);
        return template;
    }

    /// <summary>
    /// Reflection helper to set <see cref="Entity.Id"/> (which has a protected setter).
    /// Used only in tests to simulate persisted ids.
    /// </summary>
    private static void SetEntityId(Entity entity, long id)
    {
        var prop = typeof(Entity).GetProperty(
            nameof(Entity.Id),
            BindingFlags.Instance | BindingFlags.Public);
        prop!.SetValue(entity, id);
    }
}
