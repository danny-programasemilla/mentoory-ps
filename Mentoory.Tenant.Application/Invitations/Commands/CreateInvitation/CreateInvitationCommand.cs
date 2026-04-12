using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Invitations.Commands.CreateInvitation;

public sealed record CreateInvitationCommand(
    long UserId,
    Guid ProjectExternalId,
    long CreatedByUserId,
    int ExpiryHours,
    bool RequiresAcceptance = true) : IBaseRequest<Guid>;
