using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.ListProjectStages;

public sealed record ListProjectStagesQuery(long ProjectId) : IBaseRequest<IReadOnlyList<ProjectStageDto>>;
