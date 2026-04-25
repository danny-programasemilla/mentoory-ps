using FluentAssertions;
using Mentoory.Knowledge.Domain.ValueObjects;
using Xunit;

// Placeholder coverage; real Topic.ResolvePriority tests land with the Topic entity in US2 (see T069).
namespace Mentoory.Knowledge.Tests.Domain;

public class TopicTemplatePriorityResolutionTests
{
    [Fact]
    public void PriorityRange_Contains_CorrectlyClassifiesScore()
    {
        var high = PriorityRange.Create(8.00m, 10.00m);
        var medium = PriorityRange.Create(5.00m, 7.99m);
        var low = PriorityRange.Create(0.00m, 4.99m);

        high.Contains(9m).Should().BeTrue();   // -> High
        medium.Contains(6m).Should().BeTrue(); // -> Medium
        low.Contains(3m).Should().BeTrue();    // -> Low
        high.Contains(11m).Should().BeFalse(); // out of bounds -> NotApplicable
        high.Contains(-1m).Should().BeFalse();
    }
}
