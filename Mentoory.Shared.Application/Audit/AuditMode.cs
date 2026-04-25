namespace Mentoory.Shared.Application.Audit;

/// <summary>
/// Determines whether audit capture is performed automatically by the pipeline behavior
/// or manually by the handler for domain-specific detail.
/// </summary>
public enum AuditMode
{
    /// <summary>The <c>AuditingBehavior</c> writes one audit row per dispatch.</summary>
    Automatic = 0,

    /// <summary>The handler owns the audit write; the pipeline behavior is a no-op.</summary>
    Manual = 1,
}
