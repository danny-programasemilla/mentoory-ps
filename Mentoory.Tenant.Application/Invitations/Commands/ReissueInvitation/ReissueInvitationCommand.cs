using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Invitations.Commands.ReissueInvitation;

public sealed record ReissueInvitationCommand(Guid InvitationExternalId) : IBaseRequest<Guid>;
