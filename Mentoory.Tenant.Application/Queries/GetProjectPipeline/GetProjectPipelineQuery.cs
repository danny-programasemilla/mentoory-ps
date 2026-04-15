using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.GetProjectPipeline;

public sealed record GetProjectPipelineQuery(long ProjectId) : IBaseRequest<ProjectPipelineDto>;
