namespace Mentoory.Web.Infrastructure.Menu;

public static class MenuConfiguration
{
    public static IReadOnlyList<MenuItem> GetMenuItems() => new MenuItem[]
    {
        new(
            "Inicio",
            "/",
            "ti ti-home",
            new[] { "GlobalAdmin", "IncubatorAdmin", "ProjectCoordinator", "Mentor", "Entrepreneur", "Sponsor" }),

        new MenuGroup("Plataforma", "ti ti-settings", new[] { "GlobalAdmin" }, new MenuItem[]
        {
            new("Incubadoras", "/Platform/Incubators", "ti ti-building"),
            new("Usuarios", "/Platform/Users", "ti ti-users"),
            new("Plantillas", "/Platform/Templates/Diagnostics", "ti ti-file-text"),
            new("Registro de auditoría", "/Administration/AuditLog", "ti ti-history"),
        }),

        new MenuGroup("Administración", "ti ti-users-group", new[] { "ProjectCoordinator", "IncubatorAdmin", "GlobalAdmin" }, new MenuItem[]
        {
            new("Panel", "/Administration/Dashboard", "ti ti-dashboard", new[] { "IncubatorAdmin", "GlobalAdmin" }),
            new("Proyectos", "/Administration/Projects", "ti ti-sitemap", new[] { "ProjectCoordinator", "IncubatorAdmin", "GlobalAdmin" }),
            new("Usuarios", "/Administration/Users", "ti ti-users", new[] { "IncubatorAdmin", "GlobalAdmin" }),
            new("Carga Masiva", "/Administration/BatchUpload", "ti ti-file-upload", new[] { "IncubatorAdmin", "GlobalAdmin" }),
        }),

        new MenuGroup("Coordinación", "ti ti-list-check", new[] { "ProjectCoordinator", "Mentor", "IncubatorAdmin", "GlobalAdmin" }, new MenuItem[]
        {
            new("Proyectos", "/Coordination/Projects", "ti ti-sitemap"),
            new("Diagnósticos", "/Coordination/Diagnostics", "ti ti-clipboard-check"),
        }),

        new MenuGroup("Conocimiento", "ti ti-book-2", new[] { "ProjectCoordinator", "IncubatorAdmin", "GlobalAdmin" }, new MenuItem[]
        {
            new("Plantillas de conocimiento", "/Coordination/Knowledge/Templates", "ti ti-template", new[] { "GlobalAdmin" }),
            new("Estructuras del proyecto", "/Coordination/Knowledge/Projects", "ti ti-sitemap", new[] { "ProjectCoordinator", "IncubatorAdmin", "GlobalAdmin" }),
        }),

        new MenuGroup("Participante", "ti ti-school", new[] { "Entrepreneur", "Mentor", "ProjectCoordinator", "IncubatorAdmin", "GlobalAdmin" }, new MenuItem[]
        {
            new("Diagnóstico", "/Participant/Diagnostic", "ti ti-chart-bar"),
        }),
    };
}
