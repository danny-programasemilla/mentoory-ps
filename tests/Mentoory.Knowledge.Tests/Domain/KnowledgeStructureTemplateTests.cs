using FluentAssertions;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.ValueObjects;
using Xunit;

namespace Mentoory.Knowledge.Tests.Domain;

public class KnowledgeStructureTemplateTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidName_Succeeds()
    {
        var template = KnowledgeStructureTemplate.Create("Estructura Base", "Descripción", UtcNow);

        template.Name.Should().Be("Estructura Base");
        template.Description.Should().Be("Descripción");
        template.Version.Should().Be(1);
        template.IsArchived.Should().BeFalse();
        template.ExternalId.Should().NotBeEmpty();
        template.CreatedAtUtc.Should().Be(UtcNow);
        template.Modules.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        var act = () => KnowledgeStructureTemplate.Create("   ", null, UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddModule_BumpsVersion()
    {
        var template = Fresh();

        template.AddModule("Módulo 1", null, 1);

        template.Version.Should().Be(2);
        template.Modules.Should().HaveCount(1);
    }

    [Fact]
    public void AddTopic_BumpsRootVersion()
    {
        var template = Fresh();
        var module = template.AddModule("Módulo 1", null, 1);

        template.AddTopic(module.ExternalId, "Tema 1", null, 1);

        template.Version.Should().Be(3);
    }

    [Fact]
    public void Archive_BumpsVersion_AndSetsFlag()
    {
        var template = Fresh();

        template.Archive();

        template.IsArchived.Should().BeTrue();
        template.Version.Should().Be(2);
    }

    [Fact]
    public void Unarchive_BumpsVersion()
    {
        var template = Fresh();
        template.Archive();

        template.Unarchive();

        template.IsArchived.Should().BeFalse();
        template.Version.Should().Be(3);
    }

    [Fact]
    public void UpdateTopicPriorityRanges_WithOverlappingRanges_Throws()
    {
        var template = Fresh();
        var module = template.AddModule("Módulo 1", null, 1);
        var topic = template.AddTopic(module.ExternalId, "Tema 1", null, 1);
        var versionBeforeOverlap = template.Version;

        var high = PriorityRange.Create(5m, 10m);
        var medium = PriorityRange.Create(8m, 12m);

        var act = () => template.UpdateTopicPriorityRanges(topic.ExternalId, high, medium, null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*solapan*");
        template.Version.Should().Be(versionBeforeOverlap);
    }

    [Fact]
    public void RemoveModule_WithUnknownExternalId_Throws()
    {
        var template = Fresh();

        var act = () => template.RemoveModule(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Módulo no encontrado.");
    }

    [Fact]
    public void ReorderModules_WithMismatchedList_Throws()
    {
        var template = Fresh();
        var m1 = template.AddModule("Módulo 1", null, 1);
        template.AddModule("Módulo 2", null, 2);

        var act = () => template.ReorderModules(new[] { Guid.NewGuid(), m1.ExternalId });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*reordenamiento*");
    }

    private static KnowledgeStructureTemplate Fresh()
        => KnowledgeStructureTemplate.Create("Test Template", null, UtcNow);
}
