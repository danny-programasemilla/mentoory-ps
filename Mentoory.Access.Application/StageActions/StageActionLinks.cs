namespace Mentoory.Access.Application.StageActions;

public static class StageActionLinks
{
    public static string Resolve(StageGatedAction action) => action switch
    {
        StageGatedAction.DiagnosticForms => "/Coordination/Diagnostics",
        StageGatedAction.AnswerCorrection => "/Coordination/AnswerCorrection",
        StageGatedAction.LearningAssignment => "/Coordination/LearningAssignment",
        StageGatedAction.MentoringCoordination => "/Coordination/Mentoring",
        StageGatedAction.FinalEvaluation => "/Coordination/FinalEvaluation",
        StageGatedAction.Closure => "/Coordination/Closure",
        _ => "/Coordination",
    };
}
