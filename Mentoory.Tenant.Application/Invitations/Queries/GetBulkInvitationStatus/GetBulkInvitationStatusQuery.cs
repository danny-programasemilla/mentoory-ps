using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Invitations.Queries.GetBulkInvitationStatus;

public sealed record GetBulkInvitationStatusQuery(
    IReadOnlyList<long> UserIds,
    long ProjectId) : IBaseRequest<Dictionary<long, string>>;
