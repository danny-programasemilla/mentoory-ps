using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Invitations.Queries.GetUserProjectAssociations;

/// <summary>
/// Query to retrieve all project-related associations for a user,
/// including invitations and project participations.
/// </summary>
/// <param name="UserId">The internal user ID.</param>
public sealed record GetUserProjectAssociationsQuery(long UserId) : IBaseRequest<UserProjectAssociationsDto>;

/// <summary>
/// Aggregated DTO containing invitations and participations for a user.
/// </summary>
/// <param name="Invitations">List of invitation summaries.</param>
/// <param name="Participations">List of participation summaries.</param>
public sealed record UserProjectAssociationsDto(
    List<InvitationSummary> Invitations,
    List<ParticipationSummary> Participations);

/// <summary>
/// Summary of a project invitation for a user.
/// </summary>
/// <param name="ExternalId">The invitation's external GUID identifier.</param>
/// <param name="ProjectId">The project's internal ID.</param>
/// <param name="Status">The invitation status (e.g., "Pending", "Accepted", "Expired").</param>
/// <param name="ExpiresAtUtc">When the invitation expires.</param>
public sealed record InvitationSummary(
    Guid ExternalId,
    long ProjectId,
    string Status,
    DateTime ExpiresAtUtc);

/// <summary>
/// Summary of a project participation for a user.
/// </summary>
/// <param name="ExternalId">The participation's external GUID identifier.</param>
/// <param name="ProjectId">The project's internal ID.</param>
/// <param name="Role">The participant's role in the project.</param>
/// <param name="IsActive">Whether the participation is currently active.</param>
public sealed record ParticipationSummary(
    Guid ExternalId,
    long ProjectId,
    string Role,
    bool IsActive);
