using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.AdvanceProjectStage;

[Audited(AuditEventTypes.ProjectStageAdvanced, EntityType = "Project")]
public sealed record AdvanceProjectStageCommand(
    Guid ProjectExternalId,
    long ActingUserId,
    long ActingUserIncubatorId,
    bool ActingUserIsGlobalAdmin)
    : IBaseRequest<AdvanceProjectStageResult>;
