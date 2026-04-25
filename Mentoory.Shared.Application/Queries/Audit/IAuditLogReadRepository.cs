namespace Mentoory.Shared.Application.Queries.Audit;

/// <summary>
/// Read-only access to <c>[audit].[AuditLog]</c> for the admin viewer. The returned
/// queryable MUST be non-tracking so the consumer can apply filters, sorting, and
/// pagination without triggering a full materialization.
/// </summary>
public interface IAuditLogReadRepository
{
    IQueryable<AuditLogReadEntity> Query();
}
