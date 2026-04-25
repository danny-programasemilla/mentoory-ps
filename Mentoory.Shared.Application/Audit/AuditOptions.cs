namespace Mentoory.Shared.Application.Audit;

/// <summary>
/// Configuration for the audit pipeline: redacted field list, payload truncation,
/// sentinel strings. Bound from the <c>Audit</c> configuration section.
/// </summary>
public sealed class AuditOptions
{
    public const string SectionName = "Audit";

    /// <summary>
    /// Top-level property names (case-insensitive) whose values are replaced with
    /// <see cref="RedactionSentinel"/> before serialization into <c>Details</c>.
    /// </summary>
    public IReadOnlyList<string> RedactedFields { get; init; } =
    [
        "Password",
        "PasswordHash",
        "NationalId",
        "VerificationToken",
        "Token",
        "Secret",
        "ApiKey",
    ];

    /// <summary>Maximum character count for <c>Details</c>; payload is truncated with a suffix if longer.</summary>
    public int DetailsMaxCharacters { get; init; } = 8_000;

    /// <summary>Sentinel string substituted for redacted property values.</summary>
    public string RedactionSentinel { get; init; } = "***REDACTED***";

    /// <summary>Suffix appended to <c>Details</c> when the payload exceeds <see cref="DetailsMaxCharacters"/>.</summary>
    public string TruncationSuffix { get; init; } = "...<truncated>";
}
