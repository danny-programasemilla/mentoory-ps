namespace Mentoory.Access.Application.StageActions;

/// <summary>
/// Add a new member together with a <see cref="StageActionRegistry"/> entry in the same commit.
/// </summary>
public enum StageGatedAction
{
    DiagnosticForms = 0,
    AnswerCorrection = 1,
    LearningAssignment = 2,
    MentoringCoordination = 3,
    FinalEvaluation = 4,
    Closure = 5,
}
