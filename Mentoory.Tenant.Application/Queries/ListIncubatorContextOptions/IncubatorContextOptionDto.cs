namespace Mentoory.Tenant.Application.Queries.ListIncubatorContextOptions;

/// <summary>
/// Represents an incubator available for context selection, including its active projects.
/// </summary>
public sealed record IncubatorContextOptionDto(
    long IncubatorId,
    string IncubatorName,
    List<ProjectContextOptionDto> Projects);
