namespace Mentoory.Shared.Domain.SeedWork;

/// <summary>
/// Interface for the unit of work pattern.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes made in this context to the database and dispatches domain events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the changes were successfully saved and domain events were dispatched, otherwise false.</returns>
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}
