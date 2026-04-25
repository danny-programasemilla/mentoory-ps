namespace Mentoory.Knowledge.Tests;

/// <summary>
/// Deterministic UTC anchor for unit + handler tests. Use instead of
/// <see cref="DateTime.UtcNow"/> so assertions are reproducible and time-boundary
/// flakiness is impossible.
/// </summary>
internal static class TestClock
{
    public static readonly DateTime FixedUtc =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
