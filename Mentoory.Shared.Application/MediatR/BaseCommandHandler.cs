using MediatR;

namespace Mentoory.Shared.Application.MediatR;

/// <summary>
/// Base interface for MediatR requests that return a Result.
/// </summary>
public interface IBaseRequest : IRequest<Result>
{
}

/// <summary>
/// Base interface for MediatR requests that return a Result with a typed value.
/// </summary>
/// <typeparam name="T">The type of value contained in the result.</typeparam>
public interface IBaseRequest<T> : IRequest<Result<T>>
{
}

/// <summary>
/// Abstract base class for command handlers that return a typed result.
/// Provides helper methods for creating success and failure results.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled.</typeparam>
/// <typeparam name="TResult">The type of value contained in the result.</typeparam>
public abstract class BaseCommandHandler<TCommand, TResult>
    : IRequestHandler<TCommand, Result<TResult>>
    where TCommand : IRequest<Result<TResult>>
{
    /// <summary>
    /// Handles the command and returns a result.
    /// </summary>
    /// <param name="request">The command to handle.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
    public abstract Task<Result<TResult>> Handle(TCommand request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a failure result with the specified error code and messages.
    /// </summary>
    /// <param name="code">The error code indicating the type of failure.</param>
    /// <param name="messages">The error messages describing the failure.</param>
    /// <returns>A failure result.</returns>
    protected static Result<TResult> Failure(ResultErrorCodes code, params (string Context, string Message)[] messages) => Result<TResult>.Failure(code, messages);

    /// <summary>
    /// Creates a failure result with a value, error code, and messages.
    /// </summary>
    /// <param name="value">The value to include in the failure result.</param>
    /// <param name="code">The error code indicating the type of failure.</param>
    /// <param name="messages">The error messages describing the failure.</param>
    /// <returns>A failure result containing the specified value.</returns>
    protected static Result<TResult> Failure(TResult value, ResultErrorCodes code, params (string Context, string Message)[] messages) => Result<TResult>.Failure(value, code, messages);

    /// <summary>
    /// Creates a success result with the specified value.
    /// </summary>
    /// <param name="value">The value to include in the success result.</param>
    /// <returns>A success result containing the specified value.</returns>
    protected static Result<TResult> Success(TResult value) => Result.Success(value);
}
