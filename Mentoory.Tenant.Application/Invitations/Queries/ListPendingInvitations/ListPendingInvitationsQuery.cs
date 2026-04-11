using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Invitations.Queries.ListPendingInvitations;

public sealed record ListPendingInvitationsQuery(
    Guid? ProjectExternalId,
    long? UserId) : IBaseRequest<List<InvitationListItemDto>>;

public sealed record InvitationListItemDto(
    Guid ExternalId,
    long UserId,
    long ProjectId,
    string Status,
    DateTime ExpiresAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime CreatedAtUtc);
