using System.Collections.Concurrent;
using FluentValidation;
using LinaSys.Shared.Application.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Shared.Application.Behaviors;

/// <summary>
/// Pipeline behavior for validating requests using FluentValidation.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public partial class ValidatorBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidatorBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const ResultErrorCodes ErrorCode = ResultErrorCodes.Validation_SomeFieldsAreInvalid;

    private static readonly ConcurrentDictionary<Type, Func<(string, string)[], object>> _resultFactoryCache = new();

    /// <summary>
    /// Handles the validation of the request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the next delegate in the pipeline, or a validation failure result.</returns>
    /// <summary>
    /// This method performs the following steps:
    /// 1. Ensures the next delegate is not null.
    /// 2. Logs the type of the command being validated.
    /// 3. Creates a validation context for the request.
    /// 4. Executes all validators for the request asynchronously and collects the results.
    /// 5. Collects all validation failures into a list.
    /// 6. If there are no validation failures, proceeds to the next delegate in the pipeline.
    /// 7. If there are validation failures, constructs an array of error messages.
    /// 8. Checks if the response type is a generic type of Result.<> and creates a failure result with the error messages.
    /// 9. If the response type is Result (non-generic), creates a failure result with the error messages directly.
    /// 10. Throws an InvalidOperationException if the response type is neither Result<> nor Result.
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        LogValidatingCommand(request.GetGenericTypeName());

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(s => s.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Count > 0)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        var errorMessages = failures.Select(f => (f.PropertyName, f.ErrorMessage)).ToArray();

        var returnType = typeof(TResponse);
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = returnType.GetGenericArguments()[0];

            var factory = _resultFactoryCache.GetOrAdd(resultType, static type =>
            {
                var method = typeof(Result<>).MakeGenericType(type).GetMethod(nameof(Result<>.Failure), [typeof(ResultErrorCodes), typeof((string, string)[])])!;

                return messages => method.Invoke(null, [ErrorCode, messages])!;
            });

            return (TResponse)factory(errorMessages);
        }

        if (returnType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(ErrorCode, errorMessages);
        }

        throw new InvalidOperationException("Invalid return type.");
    }

    /// <summary>
    /// Logs the validation of the command.
    /// </summary>
    /// <param name="commandType">The type of the command being validated.</param>
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "[Pipeline behavior] [Validating] Command: {CommandType}")]
    partial void LogValidatingCommand(string commandType);
}
