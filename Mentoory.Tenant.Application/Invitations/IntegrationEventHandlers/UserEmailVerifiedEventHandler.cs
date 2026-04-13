using MediatR;
using Mentoory.Access.Application.IntegrationEvents;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Invitations.IntegrationEventHandlers;

public partial class UserEmailVerifiedEventHandler : INotificationHandler<UserEmailVerifiedEvent>
{
    private readonly IProjectInvitationRepository _invitationRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<UserEmailVerifiedEventHandler> _logger;

    public UserEmailVerifiedEventHandler(
        IProjectInvitationRepository invitationRepository,
        IProjectRepository projectRepository,
        ITimeProvider timeProvider,
        ILogger<UserEmailVerifiedEventHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _projectRepository = projectRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Handle(UserEmailVerifiedEvent notification, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        // Find pending invitations for this user
        var invitations = await _invitationRepository.GetByUserAsync(notification.UserId, cancellationToken);

        foreach (var invitation in invitations.Where(i => i.Status == InvitationStatus.Pending))
        {
            invitation.CheckExpiration(utcNow);
            if (invitation.Status != InvitationStatus.Pending)
            {
                continue;
            }

            // Auto-accept invitations that don't require manual acceptance
            if (!invitation.RequiresAcceptance)
            {
                var project = await _projectRepository.GetByIdAsync(invitation.ProjectId, cancellationToken);
                if (project is null)
                {
                    continue;
                }

                invitation.Accept(utcNow);
                _invitationRepository.Update(invitation);

                project.EnrollParticipant(invitation.UserId, Roles.Entrepreneur, utcNow);
                _projectRepository.Update(project);

                LogAutoAccepted(invitation.ExternalId, notification.UserId);
            }
        }

        await _invitationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Auto-accepted invitation {InvitationId} for verified user {UserId}")]
    partial void LogAutoAccepted(Guid invitationId, long userId);
}
