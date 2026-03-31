using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.EnrollParticipant;

public sealed record EnrollParticipantCommand(Guid ProjectExternalId, long UserId, string Role) : IBaseRequest<Guid>;
