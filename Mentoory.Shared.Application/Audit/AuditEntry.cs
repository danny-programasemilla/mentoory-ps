namespace Mentoory.Shared.Application.Audit;

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
    DateTime OccurredAtUtc);
