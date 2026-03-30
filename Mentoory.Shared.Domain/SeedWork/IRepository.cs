namespace Mentoory.Shared.Domain.SeedWork;

/// <summary>
/// Generic repository interface for managing aggregate roots.
/// </summary>
/// <typeparam name="T">The type of the aggregate root.</typeparam>
public interface IRepository<T>
    where T : class, IAggregateRoot
{
    /// <summary>
    /// Gets the unit of work associated with the repository.
    /// </summary>
    IUnitOfWork UnitOfWork { get; }
}
