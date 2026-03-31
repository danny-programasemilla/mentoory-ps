using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.AssignMentor;

public sealed record AssignMentorCommand(
    Guid ProjectExternalId,
    long MentorUserId,
    long EntrepreneurUserId,
    bool IsLeadMentor) : IBaseRequest<Guid>;
