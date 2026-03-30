namespace Mentoory.Shared.Application.TimeProvider;

/// <summary>
/// Default implementation of <see cref="ITimeProvider"/> using system time.
/// </summary>
public class DefaultSystemTimeProvider : ITimeProvider
{
    /// <inheritdoc />
    public DateTime Now => DateTime.Now;

    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
