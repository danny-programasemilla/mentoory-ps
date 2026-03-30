namespace Mentoory.Shared.Application.TimeProvider;

public interface ITimeProvider
{
    /// <summary>
    /// Gets the current local time.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }
}
