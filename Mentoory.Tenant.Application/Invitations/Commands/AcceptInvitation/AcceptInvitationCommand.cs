using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Invitations.Commands.AcceptInvitation;

public sealed record AcceptInvitationCommand(Guid InvitationExternalId, long UserId) : IBaseRequest;
