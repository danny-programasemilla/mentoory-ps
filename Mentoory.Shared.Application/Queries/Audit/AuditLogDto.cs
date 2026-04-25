namespace Mentoory.Shared.Application.Queries.Audit;

/// <summary>
/// DTO surfaced by <see cref="GetAuditLogPagedQuery"/> for the platform admin viewer.
/// Mirrors the <c>[audit].[AuditLog]</c> columns relevant to the read surface.
/// </summary>
public sealed record AuditLogDto(
    long Id,
    DateTime OccurredAtUtc,
    string EventType,
    string Action,
    string Outcome,
    string? UserEmail,
    string? RoleContext,
    long? UserId,
    long? IncubatorId,
    long? ProjectId,
    string? EntityType,
    string? EntityId,
    Guid? CorrelationId,
    string? ExceptionType,
    string? IpAddress,
    string? Details);
