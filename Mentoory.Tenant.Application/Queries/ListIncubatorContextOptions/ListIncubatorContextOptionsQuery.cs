using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.ListIncubatorContextOptions;

/// <summary>
/// Query to retrieve all active incubators with their active projects for GlobalAdmin context selection.
/// </summary>
public sealed record ListIncubatorContextOptionsQuery
    : IBaseRequest<List<IncubatorContextOptionDto>>;
