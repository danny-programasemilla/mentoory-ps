using Mentoory.Notification.Domain.Enums;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Notification.Application.Commands.UpdateNotificationPreference;

public sealed record UpdateNotificationPreferenceCommand(
    long UserId,
    NotificationType NotificationType,
    bool IsEnabled) : IBaseRequest;
