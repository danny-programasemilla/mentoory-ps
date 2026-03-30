using Mentoory.Example.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Example.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing Example aggregate roots.
/// </summary>
/// <param name="dbContext">The database context for accessing example data.</param>
public class ExampleRepository(ExampleDbContext dbContext)
    : AbstractRepository<Domain.Aggregates.Example.Example>(dbContext), IExampleRepository
{
    /// <summary>
    /// Retrieves all examples from the database without tracking changes.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of all examples.</returns>
    public Task<List<Domain.Aggregates.Example.Example>> GetAllAsync(CancellationToken cancellationToken)
    {
        return dbContext.Examples
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
