namespace Mentoory.Tenant.Application.Queries.ListProjects;

/// <summary>
/// Data transfer object for project list items.
/// </summary>
public sealed record ProjectDto(
    Guid ExternalId,
    long IncubatorId,
    string Name,
    string? Description,
    string CurrentStageType,
    string CurrentStageState,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
