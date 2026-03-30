using MediatR;

namespace Mentoory.Shared.Application.MediatR;

/// <summary>
/// Abstract base class for command handlers that return a non-typed result.
/// Provides helper methods for creating success and failure results.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled.</typeparam>
public abstract class BaseCommandHandler<TCommand>
    : IRequestHandler<TCommand, Result>
    where TCommand : IRequest<Result>
{
    /// <summary>
    /// Handles the command and returns a result.
    /// </summary>
    /// <param name="request">The command to handle.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
    public abstract Task<Result> Handle(TCommand request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a failure result with the specified error code and messages.
    /// </summary>
    /// <param name="code">The error code indicating the type of failure.</param>
    /// <param name="messages">The error messages describing the failure.</param>
    /// <returns>A failure result.</returns>
    protected static Result Failure(ResultErrorCodes code, params (string Context, string Message)[] messages) => Result.Failure(code, messages);

    /// <summary>
    /// Creates a success result.
    /// </summary>
    /// <returns>A success result.</returns>
    protected static Result Success() => Result.Success();
}
