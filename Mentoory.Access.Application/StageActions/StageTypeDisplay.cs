using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Access.Application.StageActions;

/// <summary>
/// Spanish display names for <see cref="StageType"/>. Constitution IX (Spanish-first UI) requires
/// all user-facing stage references to use this helper instead of the enum literal.
/// </summary>
public static class StageTypeDisplay
{
    public static string ToSpanish(StageType stage) => stage switch
    {
        StageType.Registration => "Registro",
        StageType.Forms => "Formularios",
        StageType.Analysis => "Análisis",
        StageType.LearningAssignment => "Asignación de Aprendizaje",
        StageType.Mentoring => "Mentoría",
        StageType.FinalEvaluation => "Evaluación Final",
        StageType.Closure => "Cierre",
        _ => stage.ToString(),
    };

    /// <summary>
    /// Convenience overload for call sites that hold the stage as a string (e.g., DTOs that
    /// serialize the enum with <c>.ToString()</c> for DataTables JSON). Falls back to the
    /// original string when parsing fails so display never breaks.
    /// </summary>
    public static string ToSpanish(string? stageName)
    {
        if (Enum.TryParse<StageType>(stageName, ignoreCase: false, out var stage))
        {
            return ToSpanish(stage);
        }

        return stageName ?? string.Empty;
    }
}
