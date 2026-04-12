using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;

namespace Mentoory.Tenant.Application.Invitations.Queries.GetBulkInvitationStatus;

public class GetBulkInvitationStatusHandler(
    IProjectInvitationRepository invitationRepository)
    : BaseCommandHandler<GetBulkInvitationStatusQuery, Dictionary<long, string>>
{
    public override async Task<Result<Dictionary<long, string>>> Handle(
        GetBulkInvitationStatusQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserIds.Count == 0)
        {
            return Success(new Dictionary<long, string>());
        }

        var invitations = await invitationRepository.GetByProjectAsync(request.ProjectId, cancellationToken);

        var statusByUser = invitations
            .Where(i => request.UserIds.Contains(i.UserId) && i.IsActive)
            .GroupBy(i => i.UserId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    if (g.Any(i => i.Status == InvitationStatus.Accepted))
                    {
                        return "Accepted";
                    }

                    if (g.Any(i => i.Status == InvitationStatus.Pending))
                    {
                        return "Pending";
                    }

                    return "Expired";
                });

        return Success(statusByUser);
    }
}
