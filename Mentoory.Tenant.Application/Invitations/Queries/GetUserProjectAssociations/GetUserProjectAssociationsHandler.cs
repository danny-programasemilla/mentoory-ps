using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Repositories;

namespace Mentoory.Tenant.Application.Invitations.Queries.GetUserProjectAssociations;

/// <summary>
/// Handles the GetUserProjectAssociationsQuery by loading invitations and project participations for a user.
/// </summary>
public class GetUserProjectAssociationsHandler(
    IProjectInvitationRepository invitationRepository,
    IProjectRepository projectRepository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<GetUserProjectAssociationsQuery, UserProjectAssociationsDto>
{
    /// <inheritdoc />
    public override async Task<Result<UserProjectAssociationsDto>> Handle(
        GetUserProjectAssociationsQuery request,
        CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.UtcNow;

        // Load invitations
        var invitations = await invitationRepository.GetByUserAsync(request.UserId, cancellationToken);
        foreach (var inv in invitations)
        {
            inv.CheckExpiration(utcNow);
        }

        var invitationSummaries = invitations
            .Select(i => new InvitationSummary(
                i.ExternalId,
                i.ProjectId,
                i.Status.ToString(),
                i.ExpiresAtUtc))
            .ToList();

        // Load participations via project query
        var participations = await projectRepository.ToListAsync(
            projectRepository.QueryUnfiltered()
                .SelectMany(
                    p => p.Participants,
                    (p, pp) => new { Project = p, Participant = pp })
                .Where(x => x.Participant.UserId == request.UserId)
                .Select(x => new ParticipationSummary(
                    x.Participant.ExternalId,
                    x.Project.Id,
                    x.Participant.Role,
                    x.Participant.IsActive)),
            cancellationToken);

        return Success(new UserProjectAssociationsDto(invitationSummaries, participations));
    }
}
