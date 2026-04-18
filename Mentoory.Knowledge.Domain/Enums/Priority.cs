namespace Mentoory.Knowledge.Domain.Enums;

/// <summary>
/// Resolved priority of a topic given a numeric score. Returned by <c>Topic.ResolvePriority(decimal)</c>.
/// </summary>
public enum Priority
{
    NotApplicable = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}
