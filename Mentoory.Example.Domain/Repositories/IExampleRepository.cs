using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Example.Domain.Repositories;

/// <summary>
/// Repository interface for managing Example aggregate roots.
/// </summary>
public interface IExampleRepository : IRepository<Aggregates.Example.Example>
{
    /// <summary>
    /// Adds a new example to the repository.
    /// </summary>
    /// <param name="example">The example entity to add.</param>
    /// <returns>The added example entity.</returns>
    Aggregates.Example.Example Add(Aggregates.Example.Example example);

    /// <summary>
    /// Retrieves all examples from the repository.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all examples.</returns>
    Task<List<Aggregates.Example.Example>> GetAllAsync(CancellationToken cancellationToken);
}
