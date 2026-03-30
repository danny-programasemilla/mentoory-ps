using MediatR;
using Mentoory.Shared.Infrastructure.Extensions;
using Mentoory.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mentoory.Shared.Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior that wraps request handling in a database transaction.
/// Ensures that all database operations within a request are executed atomically.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the request handler.</typeparam>
/// <param name="dbContextFactory">Factory for retrieving the appropriate database context.</param>
/// <param name="logger">Logger instance for tracking transaction lifecycle events.</param>
public partial class TransactionBehavior<TRequest, TResponse>(IDbContextFactory dbContextFactory, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string LogPrefix = "[Pipeline behavior] [Transaction] ";

    /// <summary>
    /// Handles the request by wrapping the next handler in a database transaction.
    /// If no database context is registered for the request type, the pipeline is skipped.
    /// If a transaction is already active, the existing transaction is reused.
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The response from the request handler.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = default(TResponse);
        var requestType = request.GetGenericTypeName();
        var requestName = request.GetType().FullName;

        try
        {
            if (!dbContextFactory.TryGetDbContextForRequest<TRequest>(out var dbContext))
            {
                LogSkippedTransaction(requestType, requestName!);
                return await next(cancellationToken);
            }

            if (dbContext.HasActiveTransaction)
            {
                return await next(cancellationToken);
            }

            var strategy = dbContext.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.TryBeginTransactionAsync(cancellationToken);

                using (logger.BeginScope(new List<KeyValuePair<string, object>> { new("TransactionContext", transaction.TransactionId) }))
                {
                    LogBeginTransaction(transaction.TransactionId, requestType, requestName!);

                    response = await next(cancellationToken);

                    LogCommitTransaction(transaction.TransactionId, requestType, requestName!);

                    await dbContext.CommitTransactionAsync(transaction, cancellationToken);
                }

                //// await _orderingIntegrationEventService.PublishEventsThroughEventBusAsync(transactionId);
            });

            return response!;
        }
        catch (Exception ex)
        {
            LogTransactionError(requestType, requestName!, ex);
            throw;
        }
    }

    /// <summary>
    /// Logs the beginning of a database transaction.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = LogPrefix + "Begin transaction {TransactionId} for {CommandName} ({CommandType})")]
    partial void LogBeginTransaction(Guid transactionId, string commandName, string commandType);

    /// <summary>
    /// Logs the successful commit of a database transaction.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = LogPrefix + "Commit transaction {TransactionId} for {CommandName} ({CommandType})")]
    partial void LogCommitTransaction(Guid transactionId, string commandName, string commandType);

    /// <summary>
    /// Logs an error that occurred during transaction handling.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Error, Message = LogPrefix + "Error handling transaction for for {CommandName} ({CommandType})")]
    partial void LogTransactionError(string commandName, string commandType, Exception exception);

    /// <summary>
    /// Logs a warning when a command doesn't have a registered database context and the pipeline is skipped.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = LogPrefix + "This command doesn't have a DbContext registered. Pipeline skipped. {CommandName} ({CommandType})")]
    partial void LogSkippedTransaction(string commandName, string commandType);
}
