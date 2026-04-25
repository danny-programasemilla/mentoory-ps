namespace Mentoory.Access.Application.StageActions;

public static class StageActionDisplay
{
    public static string ToSpanish(StageGatedAction action) => action switch
    {
        StageGatedAction.DiagnosticForms => "Diagnósticos",
        StageGatedAction.AnswerCorrection => "Corrección de Respuestas",
        StageGatedAction.LearningAssignment => "Asignación de Aprendizaje",
        StageGatedAction.MentoringCoordination => "Coordinación de Mentoría",
        StageGatedAction.FinalEvaluation => "Evaluación Final",
        StageGatedAction.Closure => "Cierre",
        _ => action.ToString(),
    };
}
