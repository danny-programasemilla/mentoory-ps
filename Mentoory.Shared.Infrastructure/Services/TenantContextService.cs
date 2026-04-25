using Mentoory.Shared.Application.Interfaces;

namespace Mentoory.Shared.Infrastructure.Services;

/// <summary>
/// Scoped implementation of <see cref="ITenantContext"/> populated by the tenant middleware
/// from the authenticated <see cref="System.Security.Claims.ClaimsPrincipal"/>.
/// </summary>
public class TenantContextService : ITenantContext
{
    /// <inheritdoc />
    public long? CurrentIncubatorId { get; set; }

    /// <inheritdoc />
    public long? UserId { get; set; }

    /// <inheritdoc />
    public string? UserEmail { get; set; }

    /// <inheritdoc />
    public long? ProjectId { get; set; }

    /// <inheritdoc />
    public string? Role { get; set; }
}
