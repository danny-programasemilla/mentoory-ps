using Mentoory.Notification.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Notification.Application.Queries.GetNotificationPreferences;

public class GetNotificationPreferencesHandler
    : BaseCommandHandler<GetNotificationPreferencesQuery, List<NotificationPreferenceDto>>
{
    private readonly INotificationPreferenceRepository _preferenceRepository;

    public GetNotificationPreferencesHandler(INotificationPreferenceRepository preferenceRepository)
    {
        _preferenceRepository = preferenceRepository;
    }

    public override async Task<Result<List<NotificationPreferenceDto>>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var preferences = await _preferenceRepository.GetByUserAsync(request.UserId, cancellationToken);

        var dtos = preferences
            .Select(p => new NotificationPreferenceDto(
                p.ExternalId,
                p.NotificationType,
                p.IsEnabled,
                p.UpdatedAtUtc))
            .ToList();

        return Success(dtos);
    }
}
