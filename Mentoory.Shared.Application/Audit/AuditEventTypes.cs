namespace Mentoory.Shared.Application.Audit;

/// <summary>
/// Canonical event-type constants for audit rows. Add new constants here before
/// applying <c>[Audited]</c> to a new command.
/// </summary>
public static class AuditEventTypes
{
    public const string ContextActivated = "Context.Activated";
    public const string RoleAssigned = "Role.Assigned";
    public const string MentorAssigned = "Mentor.Assigned";
    public const string UserRegistered = "User.Registered";
    public const string UserLoggedIn = "User.LoggedIn";
    public const string AnswerCorrected = "Answer.Corrected";

    // Reserved for future features — frozen now so governance surfaces them in CI.
    public const string ProjectStageAdvanced = "Project.StageAdvanced";
    public const string MentoringPlanApproved = "MentoringPlan.Approved";
}
