using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Invitations.Commands.ReissueInvitation;

public sealed record ReissueInvitationCommand(
    Guid InvitationExternalId,
    int ExpiryHours,
    long UserId,
    Guid UserExternalId,
    string Email,
    string FirstName,
    string LastName) : IBaseRequest<Guid>;
