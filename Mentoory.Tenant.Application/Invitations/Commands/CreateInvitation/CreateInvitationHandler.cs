using System.Security.Cryptography;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Aggregates.ProjectInvitation;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Invitations.Commands.CreateInvitation;

public partial class CreateInvitationHandler
    : BaseCommandHandler<CreateInvitationCommand, Guid>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectInvitationRepository _invitationRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<CreateInvitationHandler> _logger;

    public CreateInvitationHandler(
        IProjectRepository projectRepository,
        IProjectInvitationRepository invitationRepository,
        ITimeProvider timeProvider,
        ILogger<CreateInvitationHandler> logger)
    {
        _projectRepository = projectRepository;
        _invitationRepository = invitationRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override async Task<Result<Guid>> Handle(CreateInvitationCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var project = await _projectRepository.GetByExternalIdAsync(request.ProjectExternalId, cancellationToken);
        if (project is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Project", "Proyecto no encontrado."));
        }

        // Check for existing active pending invitation
        var existing = await _invitationRepository.GetActiveByUserAndProjectAsync(
            request.UserId, project.Id, cancellationToken);
        if (existing is not null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Invitation", "Ya existe una invitación activa para este usuario en este proyecto."));
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenHash = Convert.ToBase64String(tokenBytes);

        var invitation = ProjectInvitation.Create(
            project.Id,
            request.UserId,
            tokenHash,
            utcNow.AddHours(request.ExpiryHours),
            request.CreatedByUserId,
            utcNow,
            request.RequiresAcceptance);

        _invitationRepository.Add(invitation);
        await _invitationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogInvitationCreated(invitation.ExternalId, request.ProjectExternalId);

        return Success(invitation.ExternalId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation {InvitationId} created for project {ProjectId}")]
    partial void LogInvitationCreated(Guid invitationId, Guid projectId);
}
