using System.Diagnostics;
using Mentoory.Shared.Application.Interfaces;

namespace Mentoory.Shared.Infrastructure.Correlation;

/// <summary>
/// Non-HTTP fallback implementation of <see cref="ICorrelationContext"/>.
/// Reads the current <see cref="Activity"/> root id when available;
/// otherwise issues a fresh <see cref="Guid"/> per read.
/// </summary>
public sealed class AmbientCorrelationContext : ICorrelationContext
{
    public Guid CorrelationId
    {
        get
        {
            var rootId = Activity.Current?.RootId;
            return Guid.TryParse(rootId, out var guid) ? guid : Guid.NewGuid();
        }
    }

    public string? ClientIpAddress => null;
}
