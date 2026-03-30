namespace Mentoory.Shared.Application;

/// <summary>
/// Represents the result of an operation, indicating success or failure, with a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="value">The value if the operation was successful.</param>
    /// <param name="errorCode">The error code if the operation failed.</param>
    /// <param name="errorMessages">The error messages if the operation failed.</param>
    private Result(bool isSuccess, T? value = default, ResultErrorCodes? errorCode = null, (string Context, string Message)[]? errorMessages = null)
        : base(isSuccess, errorCode, errorMessages)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the value if the operation was successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Creates a failure result with a value.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="messages">The error messages.</param>
    /// <returns>A failure result with a value.</returns>
    public static new Result<T> Failure(ResultErrorCodes code, params (string Context, string Message)[] messages) => new(false, default, code, messages);

    /// <summary>
    /// Creates a failure result with a value that contains partial data.
    /// </summary>
    /// <param name="value">The partial value to include with the failure.</param>
    /// <param name="code">The error code.</param>
    /// <param name="messages">The error messages.</param>
    /// <returns>A failure result with a value.</returns>
    public static Result<T> Failure(T value, ResultErrorCodes code, params (string Context, string Message)[] messages) => new(false, value, code, messages);

    /// <summary>
    /// Creates a success result with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A success result with a value.</returns>
    public static Result<T> Success(T value) => new(true, value);
}
