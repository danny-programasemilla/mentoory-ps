using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Projects.Queries.GetProjectCurrentStage;

/// <summary>
/// At least one of <see cref="ProjectExternalId"/> or <see cref="ProjectId"/> must be supplied.
/// </summary>
public sealed record GetProjectCurrentStageQuery(
    Guid? ProjectExternalId,
    long? ProjectId)
    : IBaseRequest<GetProjectCurrentStageResult>;
