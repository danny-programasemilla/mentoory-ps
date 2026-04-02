namespace Mentoory.Tenant.Application.Queries.ListIncubatorContextOptions;

/// <summary>
/// Represents a project available for context selection within an incubator.
/// </summary>
public sealed record ProjectContextOptionDto(
    long ProjectId,
    string ProjectName);
