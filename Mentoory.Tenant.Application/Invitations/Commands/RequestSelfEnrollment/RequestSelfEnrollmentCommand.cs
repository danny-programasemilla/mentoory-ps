using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Invitations.Commands.RequestSelfEnrollment;

public sealed record RequestSelfEnrollmentCommand(Guid ProjectExternalId, long UserId) : IBaseRequest;
