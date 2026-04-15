namespace Mentoory.Access.Domain.Enums;

public enum Permission
{
    // Platform management
    ManageIncubators = 100,
    ManageSubscriptions = 101,
    ManageGlobalTemplates = 102,
    ViewAllUsers = 103,

    // Incubator administration
    ManageProjects = 200,
    ManageIncubatorUsers = 201,
    EnrollParticipants = 202,

    // Project coordination
    ManageDiagnostics = 300,
    ManageKnowledge = 301,
    ManageLifecycle = 302,
    ManageProjectParticipants = 303,
    ManageProjectPipeline = 304,
    AssignDiagnosticForms = 305,

    // Mentoring
    ManageMentoringPlans = 400,
    ManageSessions = 401,
    ManageAssignments = 402,
    CorrectAnswers = 403,
    ViewDiagnosticComparison = 404,

    // Participant
    CompleteDiagnostic = 500,
    ViewMentoringPlan = 501,
    SubmitAssignments = 502,

    // Sponsor
    ViewProjectProgress = 600,
}
