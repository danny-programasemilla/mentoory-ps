using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Invitations.Commands.AcceptInvitation;

public partial class AcceptInvitationHandler : BaseCommandHandler<AcceptInvitationCommand>
{
    private readonly IProjectInvitationRepository _invitationRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<AcceptInvitationHandler> _logger;

    public AcceptInvitationHandler(
        IProjectInvitationRepository invitationRepository,
        IProjectRepository projectRepository,
        ITimeProvider timeProvider,
        ILogger<AcceptInvitationHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _projectRepository = projectRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override async Task<Result> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var invitation = await _invitationRepository.GetByExternalIdAsync(request.InvitationExternalId, cancellationToken);
        if (invitation is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Invitation", "Invitación no encontrada."));
        }

        invitation.Accept(utcNow);
        _invitationRepository.Update(invitation);

        // Enroll participant
        var project = await _projectRepository.GetByIdAsync(invitation.ProjectId, cancellationToken);
        if (project is not null)
        {
            project.EnrollParticipant(invitation.UserId, Roles.Entrepreneur, utcNow);
            _projectRepository.Update(project);
        }

        await _invitationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogInvitationAccepted(request.InvitationExternalId);

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation {InvitationId} accepted")]
    partial void LogInvitationAccepted(Guid invitationId);
}
