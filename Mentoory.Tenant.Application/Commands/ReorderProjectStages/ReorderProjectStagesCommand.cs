using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.ReorderProjectStages;

public sealed record ReorderProjectStagesCommand(
    long ProjectId,
    IReadOnlyList<Guid> OrderedStageExternalIds) : IBaseRequest;
