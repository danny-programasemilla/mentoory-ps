namespace Mentoory.Access.Application.Configuration.Queries.ListAllConfigurations;

/// <summary>
/// Data transfer object for system configuration list items.
/// </summary>
/// <param name="Key">The configuration key.</param>
/// <param name="Value">The current configuration value.</param>
/// <param name="Description">A human-readable description of the configuration.</param>
/// <param name="DataType">The expected data type (e.g., Integer, Boolean, String).</param>
/// <param name="UpdatedAtUtc">The last modification timestamp in UTC.</param>
public sealed record ConfigurationListItemDto(
    string Key,
    string Value,
    string? Description,
    string DataType,
    DateTime UpdatedAtUtc);
