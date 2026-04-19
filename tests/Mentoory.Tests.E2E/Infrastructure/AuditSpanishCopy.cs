namespace Mentoory.Tests.E2E.Infrastructure;

/// <summary>
/// Spanish copy strings asserted by audit-related E2E tests.
/// All strings MUST match the live UI verbatim (Razor views + audit-log.js + datatable-helper.js).
/// Tests reference these constants instead of inline literals so a single source-of-truth catches
/// any UI copy drift.
/// </summary>
public static class AuditSpanishCopy
{
    // Page chrome
    public const string PageTitle = "Registro de auditoría";
    public const string RegistroDeAuditoriaMenu = "Registro de auditoría";

    // Column headers (Areas/Administration/Views/AuditLog/Index.cshtml)
    public const string FechaUtcHeader = "Fecha (UTC)";
    public const string EventoHeader = "Evento";
    public const string UsuarioHeader = "Usuario";
    public const string AccionHeader = "Acción";
    public const string ResultadoHeader = "Resultado";
    public const string RolHeader = "Rol";

    // Filter buttons (datatable-helper.js)
    public const string FiltrarButton = "Filtrar";
    public const string LimpiarButton = "Limpiar";

    // Outcome dropdown options (audit-log.js AUDIT_OUTCOME_OPTIONS)
    public const string TodosOption = "Todos";
    public const string ExitoOption = "Éxito";
    public const string FalloOption = "Fallo";

    // Pagination labels (datatable-helper.js language.paginate)
    public const string Primero = "Primero";
    public const string Siguiente = "Siguiente";
    public const string Anterior = "Anterior";
    public const string Ultimo = "Ultimo";

    // Empty state (datatable-helper.js default emptyTableHtml)
    public const string EmptyState = "No se encontraron resultados";
}
