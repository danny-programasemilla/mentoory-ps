using MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Shared.Application.IntegrationEvents;

/// <summary>
/// MediatR-based implementation of <see cref="IIntegrationEventService"/>.
/// This implementation publishes integration events as notifications using MediatR.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MediatRIntegrationEventService"/> class.
/// </remarks>
/// <param name="publisher">The MediatR publisher.</param>
/// <param name="logger">The logger.</param>
public partial class MediatRIntegrationEventService(
    ILogger<MediatRIntegrationEventService> logger,
    IPublisher publisher) : IIntegrationEventService
{
    /// <inheritdoc />
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            LogPublishingIntegrationEvent(integrationEvent.GetType().Name, integrationEvent.EventId);

            await publisher.Publish(integrationEvent, cancellationToken);

            LogIntegrationEventPublished(integrationEvent.GetType().Name, integrationEvent.EventId);
        }
        catch (Exception ex)
        {
            LogErrorPublishingIntegrationEvent(integrationEvent.GetType().Name, integrationEvent.EventId, ex);

            // In a production system, you might want to store failed events for retry
            // For now, we'll just log the error and continue
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync(IEnumerable<IIntegrationEvent> integrationEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var integrationEvent in integrationEvents)
        {
            await PublishAsync(integrationEvent, cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[IntegrationEvents] Publishing integration event {EventType} with ID {EventId}")]
    partial void LogPublishingIntegrationEvent(string eventType, Guid eventId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[IntegrationEvents] Successfully published integration event {EventType} with ID {EventId}")]
    partial void LogIntegrationEventPublished(string eventType, Guid eventId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "[IntegrationEvents] Failed to publish integration event {EventType} with ID {EventId}")]
    partial void LogErrorPublishingIntegrationEvent(string eventType, Guid eventId, Exception exception);
}
