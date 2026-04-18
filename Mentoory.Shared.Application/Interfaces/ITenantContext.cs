namespace Mentoory.Shared.Application.Interfaces;

/// <summary>
/// Ambient, per-request tenant + user context populated from the claims principal
/// by the web-layer tenant middleware.
/// </summary>
public interface ITenantContext
{
    /// <summary>Active incubator id (alias: <see cref="IncubatorId"/>). Null outside a tenant-scoped request.</summary>
    long? CurrentIncubatorId { get; }

    /// <summary>Alias for <see cref="CurrentIncubatorId"/> used by audit payloads.</summary>
    long? IncubatorId => CurrentIncubatorId;

    /// <summary>Acting user id; null for anonymous commands.</summary>
    long? UserId { get; }

    /// <summary>Acting user email; null for anonymous commands and pre-login flows.</summary>
    string? UserEmail { get; }

    /// <summary>Active project id; null when not in a project-scoped context.</summary>
    long? ProjectId { get; }

    /// <summary>Active role name (e.g., GlobalAdmin, IncubatorAdmin); null outside authenticated flows.</summary>
    string? Role { get; }
}
