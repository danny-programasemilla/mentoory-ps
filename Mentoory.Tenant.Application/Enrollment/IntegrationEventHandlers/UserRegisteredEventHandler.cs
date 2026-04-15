using MediatR;
using Mentoory.Access.Contracts.IntegrationEvents;
using Mentoory.Shared.Application.TimeProvider;
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

        // Use the event's enrollment variant (per-user override from admin toggles)
        // instead of the project's default, allowing admin to bypass on a per-user basis
        var enrollmentVariant = notification.EnrollmentVariant;

        if (enrollmentVariant == "Bypass" && !notification.RequiresVerification)
        {
            // Direct enrollment — user is verified and project uses bypass
            await _mediator.Send(
                new EnrollParticipantCommand(notification.ProjectExternalId.Value, notification.UserId, "Entrepreneur"),
                cancellationToken);

            LogDirectEnrollment(notification.UserId, notification.ProjectExternalId.Value);
        }
        else
        {
            // Create pending invitation
            await _mediator.Send(
                new CreateInvitationCommand(
                    notification.UserId,
                    notification.ProjectExternalId.Value,
                    notification.UserId, // CreatedByUserId — system-initiated
                    notification.InvitationExpiryHours),
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
