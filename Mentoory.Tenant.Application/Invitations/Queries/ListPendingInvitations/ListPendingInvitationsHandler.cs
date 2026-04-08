using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Repositories;

namespace Mentoory.Tenant.Application.Invitations.Queries.ListPendingInvitations;

public class ListPendingInvitationsHandler(
    IProjectInvitationRepository invitationRepository,
    IProjectRepository projectRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<ListPendingInvitationsQuery, List<InvitationListItemDto>>
{
    public override async Task<Result<List<InvitationListItemDto>>> Handle(
        ListPendingInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.UtcNow;
        List<Domain.Aggregates.ProjectInvitation.ProjectInvitation> invitations;

        if (request.UserId.HasValue)
        {
            invitations = await invitationRepository.GetByUserAsync(request.UserId.Value, cancellationToken);
        }
        else if (request.ProjectExternalId.HasValue)
        {
            var project = await projectRepository.GetByExternalIdAsync(request.ProjectExternalId.Value, cancellationToken);
            if (project is null)
            {
                return Success(new List<InvitationListItemDto>());
            }

            invitations = await invitationRepository.GetByProjectAsync(project.Id, cancellationToken);
        }
        else
        {
            return Success(new List<InvitationListItemDto>());
        }

        // Lazily check expiration
        foreach (var inv in invitations)
        {
            inv.CheckExpiration(utcNow);
        }

        var dtos = invitations
            .Select(i => new InvitationListItemDto(
                i.ExternalId,
                i.UserId,
                i.ProjectId,
                i.Status.ToString(),
                i.ExpiresAtUtc,
                i.AcceptedAtUtc,
                i.CreatedAtUtc))
            .ToList();

        return Success(dtos);
    }
}
