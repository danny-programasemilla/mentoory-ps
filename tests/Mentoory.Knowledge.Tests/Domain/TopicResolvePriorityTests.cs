using FluentAssertions;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.ValueObjects;
using Xunit;
using TemplateRoot = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate.KnowledgeStructureTemplate;

namespace Mentoory.Knowledge.Tests.Domain;

public class TopicResolvePriorityTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ResolvePriority_ScoreWithinHighBand_ReturnsHigh()
    {
        var topic = BuildCloneTopicWithStandardRanges();

        topic.ResolvePriority(9m).Should().Be(Priority.High);
    }

    [Fact]
    public void ResolvePriority_ScoreWithinMediumBand_ReturnsMedium()
    {
        var topic = BuildCloneTopicWithStandardRanges();

        topic.ResolvePriority(6m).Should().Be(Priority.Medium);
    }

    [Fact]
    public void ResolvePriority_ScoreWithinLowBand_ReturnsLow()
    {
        var topic = BuildCloneTopicWithStandardRanges();

        topic.ResolvePriority(3m).Should().Be(Priority.Low);
    }

    [Fact]
    public void ResolvePriority_ScoreAboveAllBands_ReturnsNotApplicable()
    {
        var topic = BuildCloneTopicWithStandardRanges();

        topic.ResolvePriority(11m).Should().Be(Priority.NotApplicable);
    }

    [Fact]
    public void ResolvePriority_ScoreBelowAllBands_ReturnsNotApplicable()
    {
        var topic = BuildCloneTopicWithStandardRanges();

        topic.ResolvePriority(-1m).Should().Be(Priority.NotApplicable);
    }

    [Fact]
    public void ResolvePriority_UsesArbitraryDecimalScale()
    {
        var structure = BuildStructureWithOneTopic(out var topicExternalId);
        structure.UpdateTopicPriorityRanges(
            topicExternalId,
            high: PriorityRange.Create(7.50m, 8.00m),
            medium: null,
            low: null);
        var topic = structure.Modules.Single().Topics.Single();

        topic.ResolvePriority(7.75m).Should().Be(Priority.High);
    }

    [Fact]
    public void ResolvePriority_WithNoRangesConfigured_ReturnsNotApplicable()
    {
        var structure = BuildStructureWithOneTopic(out _);
        var topic = structure.Modules.Single().Topics.Single();

        topic.ResolvePriority(5m).Should().Be(Priority.NotApplicable);
    }

    private static Topic BuildCloneTopicWithStandardRanges()
    {
        var structure = BuildStructureWithOneTopic(out var topicExternalId);
        structure.UpdateTopicPriorityRanges(
            topicExternalId,
            high: PriorityRange.Create(8m, 10m),
            medium: PriorityRange.Create(5m, 7.99m),
            low: PriorityRange.Create(0m, 4.99m));

        return structure.Modules.Single().Topics.Single();
    }

    private static KnowledgeStructure BuildStructureWithOneTopic(out Guid topicExternalId)
    {
        var template = TemplateRoot.Create("Plantilla", null, UtcNow);
        var moduleTemplate = template.AddModule("Módulo", null, 1);
        template.AddTopic(moduleTemplate.ExternalId, "Tema", null, 1);

        var structure = KnowledgeStructure.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);
        topicExternalId = structure.Modules.Single().Topics.Single().ExternalId;
        return structure;
    }
}
