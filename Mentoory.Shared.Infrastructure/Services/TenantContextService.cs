using Mentoory.Shared.Application.Interfaces;

namespace Mentoory.Shared.Infrastructure.Services;

/// <summary>
/// Scoped implementation of <see cref="ITenantContext"/> that gets populated by tenant middleware.
/// </summary>
public class TenantContextService : ITenantContext
{
    /// <inheritdoc />
    public long? CurrentIncubatorId { get; set; }
}
