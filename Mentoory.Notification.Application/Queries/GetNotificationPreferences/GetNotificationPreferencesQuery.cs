using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Notification.Application.Queries.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery(long UserId) : IBaseRequest<List<NotificationPreferenceDto>>;
