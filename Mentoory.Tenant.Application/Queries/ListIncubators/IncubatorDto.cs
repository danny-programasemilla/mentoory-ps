namespace Mentoory.Tenant.Application.Queries.ListIncubators;

/// <summary>
/// Data transfer object for incubator list items.
/// </summary>
public sealed record IncubatorDto(
    Guid ExternalId,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
