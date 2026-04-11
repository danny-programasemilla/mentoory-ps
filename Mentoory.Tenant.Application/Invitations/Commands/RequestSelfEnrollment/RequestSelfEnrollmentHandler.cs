using MediatR;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Domain.Enums;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Application.Commands.EnrollParticipant;
using Mentoory.Tenant.Application.Invitations.Commands.CreateInvitation;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Invitations.Commands.RequestSelfEnrollment;

/// <summary>
/// Handles self-enrollment requests from authenticated users for public projects.
/// If the project uses Bypass enrollment, the user is enrolled directly.
/// If the project uses FullFlow enrollment, a pending invitation is created.
/// </summary>
public partial class RequestSelfEnrollmentHandler : BaseCommandHandler<RequestSelfEnrollmentCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMediator _mediator;
    private readonly ISystemConfigurationReader _configReader;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<RequestSelfEnrollmentHandler> _logger;

    public RequestSelfEnrollmentHandler(
        IProjectRepository projectRepository,
        IMediator mediator,
        ISystemConfigurationReader configReader,
        ITimeProvider timeProvider,
        ILogger<RequestSelfEnrollmentHandler> logger)
    {
        _projectRepository = projectRepository;
        _mediator = mediator;
        _configReader = configReader;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(RequestSelfEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByExternalIdWithParticipantsAsync(
            request.ProjectExternalId, cancellationToken);

        if (project is null)
        {
            LogProjectNotFound(request.ProjectExternalId);
            return Failure(ResultErrorCodes.GenericError,
                ("Project", "Proyecto no encontrado."));
        }

        if (!project.IsPublic)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Project", "Este proyecto no acepta inscripciones públicas."));
        }

        if (project.CurrentStageType != StageType.Registration || project.CurrentStageState != StageState.InProgress)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Project", "Este proyecto no se encuentra en etapa de inscripción."));
        }

        var alreadyEnrolled = project.Participants.Any(p => p.UserId == request.UserId && p.IsActive);
        if (alreadyEnrolled)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("User", "Ya se encuentra inscrito en este proyecto."));
        }

        if (project.EnrollmentVariant == EnrollmentVariant.Bypass)
        {
            await _mediator.Send(
                new EnrollParticipantCommand(request.ProjectExternalId, request.UserId, "Entrepreneur"),
                cancellationToken);

            LogDirectEnrollment(request.UserId, request.ProjectExternalId);
        }
        else
        {
            var expiryHours = await _configReader.GetIntAsync(
                ConfigurationKey.InvitationTokenExpiryHours.ToString(), cancellationToken);

            await _mediator.Send(
                new CreateInvitationCommand(
                    request.UserId,
                    request.ProjectExternalId,
                    request.UserId,
                    expiryHours),
                cancellationToken);

            LogInvitationCreated(request.UserId, request.ProjectExternalId);
        }

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found: {ProjectExternalId}")]
    partial void LogProjectNotFound(Guid projectExternalId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Direct self-enrollment for user {UserId} in project {ProjectExternalId}")]
    partial void LogDirectEnrollment(long userId, Guid projectExternalId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Self-enrollment invitation created for user {UserId} in project {ProjectExternalId}")]
    partial void LogInvitationCreated(long userId, Guid projectExternalId);
}
