namespace Mentoory.Shared.Application.Queries.Audit;

/// <summary>
/// Storage-shape record for <c>[audit].[AuditLog]</c> used by the admin viewer query.
/// Lives in Application (not Infrastructure) so <see cref="IAuditLogReadRepository"/>
/// can expose an <see cref="IQueryable{T}"/> that handlers can filter and sort before
/// projecting to <see cref="AuditLogDto"/>.
/// </summary>
public sealed class AuditLogReadEntity
{
    public long Id { get; init; }

    public string EventType { get; init; } = string.Empty;

    public long? UserId { get; init; }

    public long? IncubatorId { get; init; }

    public long? ProjectId { get; init; }

    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public string Action { get; init; } = string.Empty;

    public string? Details { get; init; }

    public string? IpAddress { get; init; }

    public DateTime OccurredAtUtc { get; init; }

    public Guid? CorrelationId { get; init; }

    public string Outcome { get; init; } = string.Empty;

    public string? ExceptionType { get; init; }

    public string? UserEmail { get; init; }

    public string? RoleContext { get; init; }
}
