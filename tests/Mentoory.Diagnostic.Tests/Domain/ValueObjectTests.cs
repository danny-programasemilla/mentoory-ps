using FluentAssertions;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Domain.ValueObjects;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void ScoreContribution_WithEqualValues_ShouldBeEqual()
    {
        var a = new ScoreContribution(5.0m, SwotClassification.Strength, OdsrOrientation.Offensive);
        var b = new ScoreContribution(5.0m, SwotClassification.Strength, OdsrOrientation.Offensive);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ScoreContribution_WithDifferentScore_ShouldNotBeEqual()
    {
        var a = new ScoreContribution(5.0m, SwotClassification.Strength, OdsrOrientation.Offensive);
        var b = new ScoreContribution(3.0m, SwotClassification.Strength, OdsrOrientation.Offensive);

        a.Should().NotBe(b);
    }

    [Fact]
    public void ScoreContribution_WithDifferentSwot_ShouldNotBeEqual()
    {
        var a = new ScoreContribution(5.0m, SwotClassification.Strength, OdsrOrientation.Offensive);
        var b = new ScoreContribution(5.0m, SwotClassification.Weakness, OdsrOrientation.Offensive);

        a.Should().NotBe(b);
    }

    [Fact]
    public void ScoreContribution_WithDifferentOdsr_ShouldNotBeEqual()
    {
        var a = new ScoreContribution(5.0m, SwotClassification.Strength, OdsrOrientation.Offensive);
        var b = new ScoreContribution(5.0m, SwotClassification.Strength, OdsrOrientation.Defensive);

        a.Should().NotBe(b);
    }

    [Fact]
    public void TopicScoreAggregate_AverageScore_ShouldCalculateCorrectly()
    {
        var aggregate = new TopicScoreAggregate(topicId: 1, totalScore: 15.0m, questionCount: 3);

        aggregate.AverageScore.Should().Be(5.0m);
    }

    [Fact]
    public void TopicScoreAggregate_AverageScore_WhenQuestionCountZero_ShouldReturnZero()
    {
        var aggregate = new TopicScoreAggregate(topicId: 1, totalScore: 0, questionCount: 0);

        aggregate.AverageScore.Should().Be(0);
    }

    [Fact]
    public void TopicScoreAggregate_WithEqualValues_ShouldBeEqual()
    {
        var a = new TopicScoreAggregate(1, 10.0m, 2);
        var b = new TopicScoreAggregate(1, 10.0m, 2);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void TopicScoreAggregate_WithDifferentTopicId_ShouldNotBeEqual()
    {
        var a = new TopicScoreAggregate(1, 10.0m, 2);
        var b = new TopicScoreAggregate(2, 10.0m, 2);

        a.Should().NotBe(b);
    }

    [Fact]
    public void TopicScoreAggregate_WithDifferentTotalScore_ShouldNotBeEqual()
    {
        var a = new TopicScoreAggregate(1, 10.0m, 2);
        var b = new TopicScoreAggregate(1, 20.0m, 2);

        a.Should().NotBe(b);
    }
}
