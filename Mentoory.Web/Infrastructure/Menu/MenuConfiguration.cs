namespace Mentoory.Web.Infrastructure.Menu;

public static class MenuConfiguration
{
    public static IReadOnlyList<MenuItem> GetMenuItems() => new MenuItem[]
    {
        new(
            "Inicio",
            "/",
            "fas fa-home",
            new[] { "GlobalAdmin", "IncubatorAdmin", "ProjectCoordinator", "Mentor", "Entrepreneur", "Sponsor" }),

        new MenuGroup("Plataforma", "fas fa-cog", new[] { "GlobalAdmin" }, new MenuItem[]
        {
            new("Incubadoras", "/Platform/Incubators", "fas fa-building"),
            new("Usuarios", "/Platform/Users", "fas fa-users"),
            new("Plantillas", "/Platform/Templates/Diagnostics", "fas fa-file-alt"),
        }),

        new MenuGroup("Administración", "fas fa-users-cog", new[] { "IncubatorAdmin", "GlobalAdmin" }, new MenuItem[]
        {
            new("Panel", "/Administration/Dashboard", "fas fa-tachometer-alt"),
            new("Proyectos", "/Administration/Projects", "fas fa-project-diagram"),
            new("Usuarios", "/Administration/Users", "fas fa-users"),
            new("Carga Masiva", "/Administration/BatchUpload", "fas fa-file-upload"),
        }),

        new MenuGroup("Coordinación", "fas fa-tasks", new[] { "ProjectCoordinator", "Mentor", "IncubatorAdmin", "GlobalAdmin" }, new MenuItem[]
        {
            new("Diagnósticos", "/Coordination/Diagnostics", "fas fa-clipboard-check"),
        }),

        new MenuGroup("Participante", "fas fa-user-graduate", new[] { "Entrepreneur", "Mentor", "ProjectCoordinator", "IncubatorAdmin", "GlobalAdmin" }, new MenuItem[]
        {
            new("Diagnóstico", "/Participant/Diagnostic", "fas fa-poll"),
        }),
    };
}
