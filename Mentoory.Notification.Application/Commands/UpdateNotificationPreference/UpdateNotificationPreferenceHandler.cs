using Mentoory.Notification.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;
using NotificationPreferenceAggregate = Mentoory.Notification.Domain.Aggregates.NotificationPreference.NotificationPreference;

namespace Mentoory.Notification.Application.Commands.UpdateNotificationPreference;

public partial class UpdateNotificationPreferenceHandler : BaseCommandHandler<UpdateNotificationPreferenceCommand>
{
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<UpdateNotificationPreferenceHandler> _logger;

    public UpdateNotificationPreferenceHandler(
        INotificationPreferenceRepository preferenceRepository,
        ITimeProvider timeProvider,
        ILogger<UpdateNotificationPreferenceHandler> logger)
    {
        _preferenceRepository = preferenceRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override async Task<Result> Handle(UpdateNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var existing = await _preferenceRepository.GetByUserAndTypeAsync(
            request.UserId,
            request.NotificationType,
            cancellationToken);

        if (existing is not null)
        {
            if (existing.IsEnabled == request.IsEnabled)
            {
                return Success();
            }

            existing.UpdateEnabled(request.IsEnabled, utcNow);
            _preferenceRepository.Update(existing);
        }
        else
        {
            var preference = NotificationPreferenceAggregate.Create(
                request.UserId,
                request.NotificationType,
                request.IsEnabled,
                utcNow);

            _preferenceRepository.Add(preference);
        }

        await _preferenceRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogPreferenceUpdated(request.UserId, request.NotificationType, request.IsEnabled);

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification preference updated: UserId={UserId}, Type={Type}, Enabled={Enabled}")]
    partial void LogPreferenceUpdated(long userId, Domain.Enums.NotificationType type, bool enabled);
}
