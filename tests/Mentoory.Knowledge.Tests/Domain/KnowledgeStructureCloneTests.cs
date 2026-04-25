using FluentAssertions;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;
using Mentoory.Knowledge.Domain.Enums;
using Xunit;
using TemplateRoot = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate.KnowledgeStructureTemplate;

namespace Mentoory.Knowledge.Tests.Domain;

public class KnowledgeStructureCloneTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CloneFromTemplate_DeepCopiesTemplate_StampingEveryNode()
    {
        var template = BuildPopulatedTemplate(out var moduleExternalId, out var topicExternalId, out var subjectExternalId, out var resourceExternalId);

        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 101, incubatorId: 7, UtcNow);

        var cloneModule = clone.Modules.Single();
        cloneModule.SourceTemplateModuleExternalId.Should().Be(moduleExternalId);

        var cloneTopic = cloneModule.Topics.Single();
        cloneTopic.SourceTemplateTopicExternalId.Should().Be(topicExternalId);

        var cloneSubject = cloneTopic.Subjects.Single();
        cloneSubject.SourceTemplateSubjectExternalId.Should().Be(subjectExternalId);

        var cloneResource = cloneSubject.Resources.Single();
        cloneResource.SourceTemplateResourceExternalId.Should().Be(resourceExternalId);

        // Fresh ExternalIds — clone nodes are independent entities from their template source.
        cloneModule.ExternalId.Should().NotBe(moduleExternalId);
        cloneTopic.ExternalId.Should().NotBe(topicExternalId);
        cloneSubject.ExternalId.Should().NotBe(subjectExternalId);
        cloneResource.ExternalId.Should().NotBe(resourceExternalId);
    }

    [Fact]
    public void CloneFromTemplate_PreservesTreeShape()
    {
        var template = TemplateRoot.Create("Plantilla", null, UtcNow);
        var m1 = template.AddModule("M1", null, 1);
        var m2 = template.AddModule("M2", null, 2);
        var t1 = template.AddTopic(m1.ExternalId, "T1", null, 1);
        var t2 = template.AddTopic(m1.ExternalId, "T2", null, 2);
        var t3 = template.AddTopic(m2.ExternalId, "T3", null, 1);
        var s1 = template.AddSubject(t1.ExternalId, "S1", null, 1);
        var s2 = template.AddSubject(t3.ExternalId, "S2", null, 1);
        template.AddResource(s1.ExternalId, "R1", null, "https://x/1", ResourceType.Link, 1);
        template.AddResource(s1.ExternalId, "R2", null, "https://x/2", ResourceType.Link, 2);
        template.AddResource(s2.ExternalId, "R3", null, "https://x/3", ResourceType.Link, 1);

        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 101, incubatorId: 7, UtcNow);

        clone.Modules.Should().HaveCount(2);
        clone.Modules.SelectMany(m => m.Topics).Should().HaveCount(3);
        clone.Modules.SelectMany(m => m.Topics).SelectMany(t => t.Subjects).Should().HaveCount(2);
        clone.Modules.SelectMany(m => m.Topics).SelectMany(t => t.Subjects).SelectMany(s => s.Resources).Should().HaveCount(3);
    }

    [Fact]
    public void CloneFromTemplate_SetsSourceTemplateVersion()
    {
        var template = TemplateRoot.Create("Plantilla", null, UtcNow);
        template.AddModule("M1", null, 1); // bumps Version to 2

        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 101, incubatorId: 7, UtcNow);

        clone.SourceTemplateVersion.Should().Be(template.Version);
        clone.Name.Should().Be(template.Name);
        clone.Description.Should().Be(template.Description);
        clone.ProjectId.Should().Be(101);
        clone.IncubatorId.Should().Be(7);
        clone.CreatedAtUtc.Should().Be(UtcNow);
        clone.ExternalId.Should().NotBeEmpty();
    }

    [Fact]
    public void CloneFromTemplate_DefaultsToDisconnectedSyncMode()
    {
        var template = BuildPopulatedTemplate(out _, out _, out _, out _);

        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 101, incubatorId: 7, UtcNow);

        clone.SyncMode.Should().Be(SyncMode.Disconnected);
    }

    [Fact]
    public void AddModule_LocallyAdded_HasNullSourceStamp()
    {
        var template = BuildPopulatedTemplate(out _, out _, out _, out _);
        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 101, incubatorId: 7, UtcNow);
        var localModule = clone.AddModule("Local", null, 99);

        var found = clone.Modules.Single(m => m.ExternalId == localModule.ExternalId);

        found.SourceTemplateModuleExternalId.Should().BeNull();
    }

    [Fact]
    public void MutationsOnClone_DoNotAffectTemplate()
    {
        var template = BuildPopulatedTemplate(out _, out var templateTopicExternalId, out _, out _);
        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 101, incubatorId: 7, UtcNow);
        var cloneTopic = clone.Modules.Single().Topics.Single();

        clone.UpdateTopic(cloneTopic.ExternalId, "Renombrado en clon", "Nueva descripción");

        var templateTopic = template.Modules.Single().Topics.Single(t => t.ExternalId == templateTopicExternalId);
        templateTopic.Name.Should().Be("Tema Original");
        templateTopic.Description.Should().BeNull();

        cloneTopic.Name.Should().Be("Renombrado en clon");
        cloneTopic.Description.Should().Be("Nueva descripción");
    }

    [Fact(Skip = "No factory path in v1 produces a KnowledgeStructure with a null SourceTemplateId; Create(...) throws NotSupportedException.")]
    public void SetSyncMode_PartialSync_WhenSourceTemplateIdIsNull_Throws()
    {
        // Placeholder — reinstate once `Create` supports create-from-scratch and produces a null SourceTemplateId.
    }

    [Fact]
    public void SetSyncMode_ToPartialSync_Succeeds_WhenSourceTemplateIdIsSet()
    {
        var template = BuildPopulatedTemplate(out _, out _, out _, out _);
        var clone = KnowledgeStructure.CloneFromTemplate(template, projectId: 101, incubatorId: 7, UtcNow);

        clone.SetSyncMode(SyncMode.PartialSync);

        clone.SyncMode.Should().Be(SyncMode.PartialSync);
    }

    private static TemplateRoot BuildPopulatedTemplate(
        out Guid moduleExternalId,
        out Guid topicExternalId,
        out Guid subjectExternalId,
        out Guid resourceExternalId)
    {
        var template = TemplateRoot.Create("Plantilla Original", null, UtcNow);
        var module = template.AddModule("Módulo Original", null, 1);
        var topic = template.AddTopic(module.ExternalId, "Tema Original", null, 1);
        var subject = template.AddSubject(topic.ExternalId, "Materia Original", null, 1);
        var resource = template.AddResource(
            subject.ExternalId,
            "Recurso Original",
            null,
            "https://example.com/recurso",
            ResourceType.Link,
            1);

        moduleExternalId = module.ExternalId;
        topicExternalId = topic.ExternalId;
        subjectExternalId = subject.ExternalId;
        resourceExternalId = resource.ExternalId;
        return template;
    }
}
