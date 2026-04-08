using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Invitations.Commands.AdminAcceptInvitation;

public partial class AdminAcceptInvitationHandler : BaseCommandHandler<AdminAcceptInvitationCommand>
{
    private readonly IProjectInvitationRepository _invitationRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<AdminAcceptInvitationHandler> _logger;

    public AdminAcceptInvitationHandler(
        IProjectInvitationRepository invitationRepository,
        IProjectRepository projectRepository,
        ITimeProvider timeProvider,
        ILogger<AdminAcceptInvitationHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _projectRepository = projectRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override async Task<Result> Handle(AdminAcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var invitation = await _invitationRepository.GetByExternalIdAsync(request.InvitationExternalId, cancellationToken);
        if (invitation is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Invitation", "Invitación no encontrada."));
        }

        invitation.Accept(utcNow);
        _invitationRepository.Update(invitation);

        // Enroll participant — admin bypass, no email verification check
        var project = await _projectRepository.GetByIdAsync(invitation.ProjectId, cancellationToken);
        if (project is not null)
        {
            project.EnrollParticipant(invitation.UserId, Roles.Entrepreneur, utcNow);
            _projectRepository.Update(project);
        }

        await _invitationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogAdminAccepted(request.InvitationExternalId);

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Admin accepted invitation {InvitationId}")]
    partial void LogAdminAccepted(Guid invitationId);
}
