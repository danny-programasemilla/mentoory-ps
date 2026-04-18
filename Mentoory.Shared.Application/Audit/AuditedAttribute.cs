namespace Mentoory.Shared.Application.Audit;

/// <summary>
/// Marks a command class as subject to audit capture. Consumed by the MediatR
/// <c>AuditingBehavior</c> and enforced by <c>Mentoory.Tests.Architecture.AuditCoverageTests</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AuditedAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditedAttribute"/> class.
    /// </summary>
    /// <param name="eventType">A value from <see cref="AuditEventTypes"/>; must be non-empty.</param>
    public AuditedAttribute(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("EventType must be non-empty.", nameof(eventType));
        }

        EventType = eventType;
    }

    /// <summary>Canonical event type written to <c>[audit].[AuditLog].[EventType]</c>.</summary>
    public string EventType { get; }

    /// <summary>Optional entity-type label written to <c>[audit].[AuditLog].[EntityType]</c>.</summary>
    public string? EntityType { get; init; }

    /// <summary>Automatic (default) or Manual capture mode.</summary>
    public AuditMode Mode { get; init; } = AuditMode.Automatic;
}
