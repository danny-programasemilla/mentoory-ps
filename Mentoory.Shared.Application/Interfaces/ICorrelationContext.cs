namespace Mentoory.Shared.Application.Interfaces;

/// <summary>
/// Abstraction over the per-request correlation identifier and client IP address.
/// Keeps Application-layer code free of direct <c>IHttpContextAccessor</c> dependencies.
/// </summary>
public interface ICorrelationContext
{
    /// <summary>Correlation id shared by every audit row produced within one logical operation.</summary>
    Guid CorrelationId { get; }

    /// <summary>Remote client IP address when available; null for background/non-HTTP callers.</summary>
    string? ClientIpAddress { get; }
}
