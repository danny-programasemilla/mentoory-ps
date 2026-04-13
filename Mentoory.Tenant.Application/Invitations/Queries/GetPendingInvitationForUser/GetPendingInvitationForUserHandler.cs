using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;

namespace Mentoory.Tenant.Application.Invitations.Queries.GetPendingInvitationForUser;

public class GetPendingInvitationForUserHandler(
    IProjectInvitationRepository invitationRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<GetPendingInvitationForUserQuery, Guid?>
{
    public override async Task<Result<Guid?>> Handle(
        GetPendingInvitationForUserQuery request,
        CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetActiveByUserAndProjectAsync(
            request.UserId, request.ProjectId, cancellationToken);

        if (invitation is null || invitation.Status != InvitationStatus.Pending)
        {
            return Success((Guid?)null);
        }

        invitation.CheckExpiration(timeProvider.UtcNow);
        if (invitation.Status != InvitationStatus.Pending)
        {
            return Success((Guid?)null);
        }

        return Success((Guid?)invitation.ExternalId);
    }
}
