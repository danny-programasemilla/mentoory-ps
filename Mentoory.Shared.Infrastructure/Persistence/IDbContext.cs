using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mentoory.Shared.Infrastructure.Persistence;

/// <summary>
/// Interface for the database context, providing methods for transaction management and execution strategy.
/// </summary>
public interface IDbContext
{
    /// <summary>
    /// Gets the <see cref="DatabaseFacade"/> instance for the context.
    /// </summary>
    DatabaseFacade Database { get; }

    /// <summary>
    /// Gets a value indicating whether there is an active transaction.
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Begins a new transaction asynchronously if there is no active transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new transaction.</returns>
    Task<IDbContextTransaction> TryBeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction asynchronously.
    /// </summary>
    /// <param name="transaction">The transaction to commit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an execution strategy for the database operations.
    /// </summary>
    /// <returns>An execution strategy.</returns>
    IExecutionStrategy CreateExecutionStrategy();
}
