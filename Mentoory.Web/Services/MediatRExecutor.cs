using MediatR;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Web.Services;

/// <summary>
/// Provides methods to execute MediatR requests with logging and error handling.
/// </summary>
public class MediatRExecutor(IMediator mediator, ILogger<MediatRExecutor> logger, IHostEnvironment environment)
{
    /// <summary>
    /// Sends a request using MediatR and logs any failures.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> indicating success or failure.</returns>
    public async Task<Result<T>> SendAndLogIfFailureAsync<T>(IBaseRequest<T> request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mediator.Send(request, cancellationToken);

            if (!result.IsSuccess)
            {
                LogFailure(request, result.ErrorCode?.ToString(), result.ErrorMessages);
            }

            return result;
        }
        catch (Exception ex)
        {
            if (environment.IsDevelopment())
            {
                throw;
            }

            logger.LogError(ex, "Unhandled exception in request {RequestType}", request.GetType().Name);
            return Result<T>.Failure(ResultErrorCodes.Unknown, (nameof(SendAndLogIfFailureAsync), "An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Sends a request using MediatR and logs any failures.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public async Task<Result> SendAndLogIfFailureAsync(Mentoory.Shared.Application.MediatR.IBaseRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mediator.Send(request, cancellationToken);

            if (!result.IsSuccess)
            {
                LogFailure(request, result.ErrorCode?.ToString(), result.ErrorMessages);
            }

            return result;
        }
        catch (Exception ex)
        {
            if (environment.IsDevelopment())
            {
                throw;
            }

            logger.LogError(ex, "Unhandled exception in request {RequestType}", request.GetType().Name);

            return Result.Failure(ResultErrorCodes.Unknown, (nameof(SendAndLogIfFailureAsync), "An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Sends a request using MediatR and throws an exception if the request fails.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result value if the request succeeds.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the request fails or an unexpected error occurs.</exception>
    public async Task<T> SendOrThrowAsync<T>(IBaseRequest<T> request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mediator.Send(request, cancellationToken);

            if (result.IsSuccess)
            {
                return result.Value!;
            }

            HandleFailure(request, result.ErrorCode?.ToString(), result.ErrorMessages);
        }
        catch (Exception ex)
        {
            if (environment.IsDevelopment())
            {
                throw;
            }

            logger.LogError(ex, "Unhandled exception in request {RequestType}", request.GetType().Name);
            throw new InvalidOperationException("An unexpected error occurred while processing the request.");
        }

        // Unreachable, but required by compiler
        throw new InvalidOperationException("Unexpected flow.");
    }

    /// <summary>
    /// Sends a request using MediatR and throws an exception if the request fails.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown if the request fails or an unexpected error occurs.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SendOrThrowAsync(Mentoory.Shared.Application.MediatR.IBaseRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mediator.Send(request, cancellationToken);

            if (result.IsSuccess)
            {
                return;
            }

            HandleFailure(request, result.ErrorCode?.ToString(), result.ErrorMessages);
        }
        catch (Exception ex)
        {
            if (environment.IsDevelopment())
            {
                throw;
            }

            logger.LogError(ex, "Unhandled exception in request {RequestType}", request.GetType().Name);
            throw new InvalidOperationException("An unexpected error occurred while processing the request.");
        }
    }

    /// <summary>
    /// Handles a failed request by logging the failure and throwing an exception.
    /// </summary>
    /// <param name="request">The request that failed.</param>
    /// <param name="errorCode">The error code associated with the failure.</param>
    /// <param name="errorMessages">The error messages associated with the failure.</param>
    /// <exception cref="InvalidOperationException">Always thrown to indicate the failure.</exception>
    private void HandleFailure(object request, string? errorCode, (string Context, string Message)[]? errorMessages)
    {
        var typeName = request.GetType().Name;
        var message = $"{typeName} failed";
        var code = errorCode ?? "UnknownError";

        // If the error messages are not null or empty, join them into a single string
        // Otherwise, set a default message
        var messages = errorMessages?.Length > 0
            ? string.Join(", ", errorMessages.Select(m => $"{m.Context}: {m.Message}"))
            : "No error messages";

        logger.LogError("Request {RequestType} failed. Code: {ErrorCode}. Messages: {Messages}", typeName, code, messages);

        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Logs a failed request without throwing an exception.
    /// </summary>
    /// <param name="request">The request that failed.</param>
    /// <param name="errorCode">The error code associated with the failure.</param>
    /// <param name="errorMessages">The error messages associated with the failure.</param>
    private void LogFailure(object request, string? errorCode, (string Context, string Message)[]? errorMessages)
    {
        var typeName = request.GetType().Name;
        var code = errorCode ?? "UnknownError";

        // If the error messages are not null or empty, join them into a single string
        // Otherwise, set a default message
        var messages = errorMessages?.Length > 0
            ? string.Join(", ", errorMessages.Select(m => $"{m.Context}: {m.Message}"))
            : "No error messages";

        logger.LogError("Request {RequestType} failed. Code: {ErrorCode}. Messages: {Messages}", typeName, code, messages);
    }
}
