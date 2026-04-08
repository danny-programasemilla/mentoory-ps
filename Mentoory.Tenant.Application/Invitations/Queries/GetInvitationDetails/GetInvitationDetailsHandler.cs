using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Repositories;

namespace Mentoory.Tenant.Application.Invitations.Queries.GetInvitationDetails;

public class GetInvitationDetailsHandler(
    IProjectInvitationRepository invitationRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<GetInvitationDetailsQuery, InvitationDetailsDto>
{
    public override async Task<Result<InvitationDetailsDto>> Handle(
        GetInvitationDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByExternalIdAsync(request.InvitationExternalId, cancellationToken);
        if (invitation is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Invitation", "Invitación no encontrada."));
        }

        invitation.CheckExpiration(timeProvider.UtcNow);

        return Success(new InvitationDetailsDto(
            invitation.ExternalId,
            invitation.UserId,
            invitation.ProjectId,
            invitation.Status.ToString(),
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc,
            invitation.CreatedAtUtc,
            invitation.IsActive));
    }
}
