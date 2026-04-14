using System.Security.Cryptography;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Contracts.IntegrationEvents;
using Mentoory.Tenant.Domain.Aggregates.ProjectInvitation;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Invitations.Commands.ReissueInvitation;

public partial class ReissueInvitationHandler : BaseCommandHandler<ReissueInvitationCommand, Guid>
{
    private readonly IProjectInvitationRepository _invitationRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IIncubatorRepository _incubatorRepository;
    private readonly IIntegrationEventService _eventService;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<ReissueInvitationHandler> _logger;

    public ReissueInvitationHandler(
        IProjectInvitationRepository invitationRepository,
        IProjectRepository projectRepository,
        IIncubatorRepository incubatorRepository,
        IIntegrationEventService eventService,
        ITimeProvider timeProvider,
        ILogger<ReissueInvitationHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _projectRepository = projectRepository;
        _incubatorRepository = incubatorRepository;
        _eventService = eventService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override async Task<Result<Guid>> Handle(ReissueInvitationCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var oldInvitation = await _invitationRepository.GetByExternalIdAsync(request.InvitationExternalId, cancellationToken);
        if (oldInvitation is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Invitation", "Invitación no encontrada."));
        }

        oldInvitation.Deactivate();
        _invitationRepository.Update(oldInvitation);

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenHash = Convert.ToBase64String(tokenBytes);

        var newInvitation = ProjectInvitation.Create(
            oldInvitation.ProjectId,
            oldInvitation.UserId,
            tokenHash,
            utcNow.AddHours(request.ExpiryHours),
            oldInvitation.CreatedByUserId,
            utcNow);

        _invitationRepository.Add(newInvitation);
        await _invitationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogInvitationReissued(request.InvitationExternalId, newInvitation.ExternalId);

        await PublishInvitationReissuedEventAsync(oldInvitation, request, utcNow, cancellationToken);

        return Success(newInvitation.ExternalId);
    }

    private async Task PublishInvitationReissuedEventAsync(
        ProjectInvitation invitation,
        ReissueInvitationCommand request,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(invitation.ProjectId, cancellationToken);
        if (project is null)
        {
            LogProjectNotFoundForEvent(invitation.ProjectId);
            return;
        }

        var incubator = await _incubatorRepository.GetByIdAsync(project.IncubatorId, cancellationToken);
        var incubatorName = incubator?.Name ?? "Incubadora";

        await _eventService.PublishAsync(
            new InvitationReissuedEvent(
                request.UserId,
                request.UserExternalId,
                request.Email,
                request.FirstName,
                request.LastName,
                project.ExternalId,
                project.Name,
                incubatorName,
                request.ExpiryHours,
                utcNow),
            cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation {OldId} reissued as {NewId}")]
    partial void LogInvitationReissued(Guid oldId, Guid newId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project not found for invitation reissue event: ProjectId={ProjectId}")]
    partial void LogProjectNotFoundForEvent(long projectId);
}
