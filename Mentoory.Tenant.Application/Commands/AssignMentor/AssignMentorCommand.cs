using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.AssignMentor;

[Audited(AuditEventTypes.MentorAssigned, EntityType = "MentorAssignment")]
public sealed record AssignMentorCommand(
    Guid ProjectExternalId,
    long MentorUserId,
    long EntrepreneurUserId,
    bool IsLeadMentor) : IBaseRequest<Guid>;
