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
        }),

        new MenuGroup("Administración", "ti ti-users-group", new[] { "IncubatorAdmin", "GlobalAdmin" }, new MenuItem[]
        {
            new("Panel", "/Administration/Dashboard", "ti ti-dashboard"),
            new("Proyectos", "/Administration/Projects", "ti ti-sitemap"),
            new("Usuarios", "/Administration/Users", "ti ti-users"),
            new("Carga Masiva", "/Administration/BatchUpload", "ti ti-file-upload"),
        }),

        new MenuGroup("Coordinación", "ti ti-list-check", new[] { "ProjectCoordinator", "Mentor", "IncubatorAdmin", "GlobalAdmin" }, new MenuItem[]
        {
            new("Etapas", "/Coordination/ProjectPipeline", "ti ti-route"),
            new("Diagnósticos", "/Coordination/Diagnostics", "ti ti-clipboard-check"),
            new("Resultados", "/Coordination/DiagnosticResults", "ti ti-chart-arrows"),
        }),

        new MenuGroup("Participante", "ti ti-school", new[] { "Entrepreneur", "Mentor", "ProjectCoordinator", "IncubatorAdmin", "GlobalAdmin" }, new MenuItem[]
        {
            new("Diagnóstico", "/Participant/Diagnostic", "ti ti-chart-bar"),
        }),
    };
}
