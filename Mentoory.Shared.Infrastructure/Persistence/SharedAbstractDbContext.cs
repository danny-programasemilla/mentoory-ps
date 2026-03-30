using System.Data;
using MediatR;
using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Shared.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mentoory.Shared.Infrastructure.Persistence;

/// <summary>
/// Abstract base class for Entity Framework Core database contexts that provides
/// transaction management and unit of work functionality with MediatR integration.
/// </summary>
/// <param name="options">The options to be used by the DbContext.</param>
/// <param name="mediator">The MediatR mediator instance for dispatching domain events.</param>
public abstract class SharedAbstractDbContext(DbContextOptions options, IMediator mediator)
    : DbContext(options), IDbContext, IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// Gets a value indicating whether there is an active transaction in the current scope.
    /// </summary>
    public bool HasActiveTransaction => _currentTransaction is not null;

    /// <summary>
    /// Commits the specified transaction after saving all pending changes.
    /// </summary>
    /// <param name="transaction">The transaction to commit.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="transaction"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the transaction is not the one in the current scope.</exception>
    public async Task CommitTransactionAsync(IDbContextTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction != _currentTransaction)
        {
            throw new InvalidOperationException(
                $"Transaction {transaction.TransactionId} is not the one in the current scope");
        }

        try
        {
            await SaveEntitiesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    /// <summary>
    /// Creates a new execution strategy for handling database operations with retry logic.
    /// </summary>
    /// <returns>An execution strategy instance.</returns>
    public IExecutionStrategy CreateExecutionStrategy()
    {
        return Database.CreateExecutionStrategy();
    }

    /// <summary>
    /// Rolls back the current transaction and disposes it.
    /// </summary>
    public void RollbackTransaction()
    {
        try
        {
            _currentTransaction?.Rollback();
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    /// <summary>
    /// Saves all changes made in this context to the database after dispatching domain events.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result indicates whether the save was successful.</returns>
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await mediator.DispatchDomainEventsAsync(this);
        _ = await this.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Begins a new database transaction if one is not already active.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the database transaction.</returns>
    public async Task<IDbContextTransaction> TryBeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return _currentTransaction ??=
            await Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, cancellationToken: cancellationToken);
    }
}
