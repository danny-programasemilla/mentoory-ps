using System.Reflection;
using FluentAssertions;
using Mentoory.Knowledge.Application.Commands.SyncFromTemplate;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Domain.SeedWork;
using Moq;
using Xunit;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Tests.Handlers;

/// <summary>
/// Domain-level round-trip for US5 partial sync. Exercises the full chain:
/// <c>Template.AddTopic → Clone.SetSyncMode(PartialSync) → Handler.Handle → Clone reflects new topic</c>
/// using in-memory mocks only (no DbContext). A full DB-level round-trip (T103) lives in the
/// integration test suite and is skipped here to keep this project lightweight.
/// </summary>
public class PartialSyncRoundTripTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact(Skip = "Integration test — see T103 for full DB-level round-trip.")]
    public void RoundTrip_OverDbContext_IsCoveredByIntegrationSuite()
    {
        // Placeholder: the full round-trip through KnowledgeDbContext + Respawn lives in
        // tests/Mentoory.Tests.Integration/Knowledge/ (future sibling to
        // DiagnosticCascadeRoundTripTests.cs). Keeping the skipped fact here documents the
        // coverage gap and makes it easy to flip on once the integration harness grows.
    }

    [Fact]
    public async Task RoundTrip_InMemory_TemplateAdditionFlowsIntoCloneViaHandler()
    {
        // Arrange: build a template + its clone in memory, register them in mocked repositories.
        var template = BuildTemplate(templateId: 1_000);
        var structure = KS.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);
        structure.SetSyncMode(SyncMode.PartialSync);

        var structureRepo = new Mock<IKnowledgeStructureRepository>();
        var templateRepo = new Mock<IKnowledgeStructureTemplateRepository>();
        var uow = new Mock<IUnitOfWork>();

        structureRepo.Setup(r => r.UnitOfWork).Returns(uow.Object);
        uow.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        structureRepo
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(structure.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);
        templateRepo
            .Setup(r => r.GetByIdWithFullTreeAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Template evolves: adds a module, a topic, a subject, and a resource after clone was taken.
        var moduleExternalId = template.Modules.Single().ExternalId;
        var topicExternalId = template.Modules.Single().Topics.Single().ExternalId;
        var subjectExternalId = template.Modules.Single().Topics.Single().Subjects.Single().ExternalId;

        template.AddTopic(moduleExternalId, "Tema Nuevo", null, 2);
        template.AddSubject(topicExternalId, "Materia Nueva", null, 2);
        template.AddResource(
            subjectExternalId,
            "Recurso Nuevo",
            null,
            "https://example.com/new",
            ResourceType.Link,
            2);

        var handler = new SyncFromTemplateHandler(structureRepo.Object, templateRepo.Object);

        // Act
        var result = await handler.Handle(
            new SyncFromTemplateCommand(structure.ExternalId),
            CancellationToken.None);

        // Assert — handler succeeded, counts reflect additions, and the clone now sees them.
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TopicsAdded.Should().Be(1);
        result.Value.SubjectsAdded.Should().Be(1);
        result.Value.ResourcesAdded.Should().Be(1);

        structure.Modules.Single().Topics.Should().HaveCount(2);
        structure.Modules.Single().Topics.Single(t => t.Name == "Tema Nuevo")
            .SourceTemplateTopicExternalId.Should().NotBeNull();
        structure.Modules.Single().Topics.Single(t => t.Name == "Tema 1").Subjects
            .Should().Contain(s => s.Name == "Materia Nueva");

        uow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static KnowledgeStructureTemplate BuildTemplate(long templateId)
    {
        var template = KnowledgeStructureTemplate.Create("Plantilla", "desc", UtcNow);
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

        var prop = typeof(Entity).GetProperty(
            nameof(Entity.Id),
            BindingFlags.Instance | BindingFlags.Public);
        prop!.SetValue(template, templateId);
        return template;
    }
}
