using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Invitations.Queries.GetPendingInvitationForUser;

public sealed record GetPendingInvitationForUserQuery(
    long UserId,
    long ProjectId) : IBaseRequest<Guid?>;
