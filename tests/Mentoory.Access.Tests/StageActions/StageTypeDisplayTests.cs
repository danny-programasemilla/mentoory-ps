using FluentAssertions;
using Mentoory.Access.Application.StageActions;
using Mentoory.Tenant.Domain.Enums;
using Xunit;

namespace Mentoory.Access.Tests.StageActions;

public class StageTypeDisplayTests
{
    [Theory]
    [InlineData(StageType.Registration, "Registro")]
    [InlineData(StageType.Forms, "Formularios")]
    [InlineData(StageType.Analysis, "Análisis")]
    [InlineData(StageType.LearningAssignment, "Asignación de Aprendizaje")]
    [InlineData(StageType.Mentoring, "Mentoría")]
    [InlineData(StageType.FinalEvaluation, "Evaluación Final")]
    [InlineData(StageType.Closure, "Cierre")]
    public void ToSpanish_ReturnsExpectedDisplayName(StageType stage, string expected)
    {
        StageTypeDisplay.ToSpanish(stage).Should().Be(expected);
    }

    [Fact]
    public void ToSpanish_CoversAllDefinedStages()
    {
        foreach (StageType stage in Enum.GetValues<StageType>())
        {
            StageTypeDisplay.ToSpanish(stage).Should().NotBeNullOrWhiteSpace();
        }
    }
}
