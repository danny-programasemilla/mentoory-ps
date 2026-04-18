using FluentAssertions;
using Mentoory.Knowledge.Domain.ValueObjects;
using Xunit;

namespace Mentoory.Knowledge.Tests.Domain;

public class PriorityRangeTests
{
    [Fact]
    public void Create_WithValidBounds_ReturnsRangeWithSetValues()
    {
        var range = PriorityRange.Create(5m, 10m);

        range.Min.Should().Be(5m);
        range.Max.Should().Be(10m);
    }

    [Fact]
    public void Create_WithEqualMinAndMax_Succeeds()
    {
        var range = PriorityRange.Create(7m, 7m);

        range.Min.Should().Be(7m);
        range.Max.Should().Be(7m);
    }

    [Fact]
    public void Create_WithMinGreaterThanMax_ThrowsArgumentException()
    {
        Action act = () => PriorityRange.Create(10m, 5m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Min must be less than or equal to Max.*");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(10)]
    public void Contains_WhenScoreWithinInclusiveBounds_ReturnsTrue(decimal score)
    {
        var range = PriorityRange.Create(5m, 10m);

        range.Contains(score).Should().BeTrue();
    }

    [Theory]
    [InlineData(4.99)]
    [InlineData(10.01)]
    public void Contains_WhenScoreOutsideBounds_ReturnsFalse(decimal score)
    {
        var range = PriorityRange.Create(5m, 10m);

        range.Contains(score).Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_NonOverlappingRanges_ReturnsFalse()
    {
        var a = PriorityRange.Create(0m, 5m);
        var b = PriorityRange.Create(6m, 10m);

        a.OverlapsWith(b).Should().BeFalse();
        b.OverlapsWith(a).Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_PartiallyOverlappingRanges_ReturnsTrue()
    {
        var a = PriorityRange.Create(0m, 5m);
        var b = PriorityRange.Create(4m, 10m);

        a.OverlapsWith(b).Should().BeTrue();
        b.OverlapsWith(a).Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_TouchingEndpoints_ReturnsTrue()
    {
        var a = PriorityRange.Create(0m, 10m);
        var b = PriorityRange.Create(10m, 20m);

        a.OverlapsWith(b).Should().BeTrue();
        b.OverlapsWith(a).Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_FullyContainedRange_ReturnsTrue()
    {
        var inner = PriorityRange.Create(3m, 7m);
        var outer = PriorityRange.Create(0m, 10m);

        inner.OverlapsWith(outer).Should().BeTrue();
        outer.OverlapsWith(inner).Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_Null_ReturnsFalse()
    {
        var range = PriorityRange.Create(0m, 10m);

        range.OverlapsWith(null).Should().BeFalse();
    }

    [Fact]
    public void RecordEquality_TwoRangesWithSameBounds_AreEqual()
    {
        var a = PriorityRange.Create(1m, 2m);
        var b = PriorityRange.Create(1m, 2m);

        (a == b).Should().BeTrue();
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
