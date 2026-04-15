using Mentoory.Notification.Domain.Enums;
using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Notification.Contracts.IntegrationEvents;

public sealed record NotificationRequestedEvent(
    NotificationType NotificationType,
    string Subject,
    string HtmlBody,
    long RecipientUserId,
    string RecipientEmail,
    Guid? SourceEventId,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
