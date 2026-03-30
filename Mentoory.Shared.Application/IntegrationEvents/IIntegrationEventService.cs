namespace Mentoory.Shared.Application.IntegrationEvents;

/// <summary>
/// Service for publishing integration events.
/// This abstraction allows for different implementations (MediatR, RabbitMQ, etc.)
/// without changing the consuming code.
/// </summary>
public interface IIntegrationEventService
{
    /// <summary>
    /// Publishes an integration event asynchronously.
    /// </summary>
    /// <param name="integrationEvent">The integration event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple integration events asynchronously.
    /// </summary>
    /// <param name="integrationEvents">The integration events to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(IEnumerable<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken = default);
}
