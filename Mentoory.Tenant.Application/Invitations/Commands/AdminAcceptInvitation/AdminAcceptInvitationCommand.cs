using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Invitations.Commands.AdminAcceptInvitation;

public sealed record AdminAcceptInvitationCommand(Guid InvitationExternalId) : IBaseRequest;
