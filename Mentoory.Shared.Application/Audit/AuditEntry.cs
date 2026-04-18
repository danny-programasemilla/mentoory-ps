namespace Mentoory.Shared.Application.Audit;

/// <summary>
/// Immutable record describing one audit-log entry. Extended in feature 016 with
/// correlation, outcome, exception, user email, and role context fields.
/// </summary>
public sealed record AuditEntry(
    string EventType,
    long? UserId,
    long? IncubatorId,
    long? ProjectId,
    string? EntityType,
    string? EntityId,
    string Action,
    string? Details,
    string? IpAddress,
    DateTime OccurredAtUtc,
    Guid? CorrelationId,
    string Outcome,
    string? ExceptionType,
    string? UserEmail,
    string? RoleContext);
