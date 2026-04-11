namespace Mentoory.Shared.Domain.Constants;

public static class Roles
{
    public const string GlobalAdmin = "GlobalAdmin";
    public const string IncubatorAdmin = "IncubatorAdmin";
    public const string ProjectCoordinator = "ProjectCoordinator";
    public const string Mentor = "Mentor";
    public const string Entrepreneur = "Entrepreneur";
    public const string Sponsor = "Sponsor";

    public static readonly IReadOnlyList<string> All = new[]
    {
        GlobalAdmin,
        IncubatorAdmin,
        ProjectCoordinator,
        Mentor,
        Entrepreneur,
        Sponsor,
    };
}
