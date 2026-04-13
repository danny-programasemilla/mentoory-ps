using MediatR;
using Mentoory.Access.Application.IntegrationEvents;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Application.Commands.EnrollParticipant;
using Mentoory.Tenant.Application.Invitations.Commands.CreateInvitation;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Enrollment.IntegrationEventHandlers;

public partial class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly IMediator _mediator;
    private readonly IProjectRepository _projectRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<UserRegisteredEventHandler> _logger;

    public UserRegisteredEventHandler(
        IMediator mediator,
        IProjectRepository projectRepository,
        ITimeProvider timeProvider,
        ILogger<UserRegisteredEventHandler> logger)
    {
        _mediator = mediator;
        _projectRepository = projectRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        if (notification.ProjectExternalId is null)
        {
            return; // Public self-registration — no project association yet
        }

        var project = await _projectRepository.GetByExternalIdAsync(notification.ProjectExternalId.Value, cancellationToken);
        if (project is null)
        {
            LogProjectNotFound(notification.ProjectExternalId.Value);
            return;
        }

        var isBypass = notification.EnrollmentVariant == EnrollmentVariants.Bypass;

        if (isBypass && !notification.RequiresVerification)
        {
            // Direct enrollment — user is verified and bypass requested
            await _mediator.Send(
                new EnrollParticipantCommand(notification.ProjectExternalId.Value, notification.UserId, "Entrepreneur"),
                cancellationToken);

            LogDirectEnrollment(notification.UserId, notification.ProjectExternalId.Value);
        }
        else
        {
            // Compute RequiresAcceptance from event fields:
            // FullFlow → user must manually accept (RequiresAcceptance = true)
            // Bypass → auto-accept after verification (RequiresAcceptance = false)
            var requiresAcceptance = !isBypass;

            await _mediator.Send(
                new CreateInvitationCommand(
                    notification.UserId,
                    notification.ProjectExternalId.Value,
                    notification.UserId, // CreatedByUserId — system-initiated
                    notification.InvitationExpiryHours,
                    requiresAcceptance),
                cancellationToken);

            LogInvitationCreated(notification.UserId, notification.ProjectExternalId.Value);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found: {ProjectId}")]
    partial void LogProjectNotFound(Guid projectId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Direct enrollment for user {UserId} in project {ProjectId}")]
    partial void LogDirectEnrollment(long userId, Guid projectId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation created for user {UserId} in project {ProjectId}")]
    partial void LogInvitationCreated(long userId, Guid projectId);
}
