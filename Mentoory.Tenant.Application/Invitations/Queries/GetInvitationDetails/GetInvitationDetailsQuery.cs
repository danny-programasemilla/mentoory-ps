using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Invitations.Queries.GetInvitationDetails;

public sealed record GetInvitationDetailsQuery(Guid InvitationExternalId) : IBaseRequest<InvitationDetailsDto>;

public sealed record InvitationDetailsDto(
    Guid ExternalId,
    long UserId,
    long ProjectId,
    string Status,
    DateTime ExpiresAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime CreatedAtUtc,
    bool IsActive);
