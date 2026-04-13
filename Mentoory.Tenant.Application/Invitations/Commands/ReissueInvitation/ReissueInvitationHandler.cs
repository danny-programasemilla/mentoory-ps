using System.Security.Cryptography;
using Mentoory.Tenant.Application.Configuration;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Aggregates.ProjectInvitation;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Invitations.Commands.ReissueInvitation;

public partial class ReissueInvitationHandler : BaseCommandHandler<ReissueInvitationCommand, Guid>
{
    private readonly IProjectInvitationRepository _invitationRepository;
    private readonly ISystemConfigurationReader _configReader;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<ReissueInvitationHandler> _logger;

    public ReissueInvitationHandler(
        IProjectInvitationRepository invitationRepository,
        ISystemConfigurationReader configReader,
        ITimeProvider timeProvider,
        ILogger<ReissueInvitationHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _configReader = configReader;
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

        var expiryHours = await _configReader.GetIntAsync(
            nameof(TenantConfigurationKey.InvitationTokenExpiryHours), cancellationToken);

        // Deactivate old invitation
        oldInvitation.Deactivate();
        _invitationRepository.Update(oldInvitation);

        // Create new invitation
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenHash = Convert.ToBase64String(tokenBytes);

        var newInvitation = ProjectInvitation.Create(
            oldInvitation.ProjectId,
            oldInvitation.UserId,
            tokenHash,
            utcNow.AddHours(expiryHours),
            oldInvitation.CreatedByUserId,
            utcNow);

        _invitationRepository.Add(newInvitation);
        await _invitationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogInvitationReissued(request.InvitationExternalId, newInvitation.ExternalId);

        return Success(newInvitation.ExternalId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation {OldId} reissued as {NewId}")]
    partial void LogInvitationReissued(Guid oldId, Guid newId);
}
