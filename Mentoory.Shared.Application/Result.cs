namespace Mentoory.Shared.Application;

/// <summary>
/// Represents the result of an operation, indicating success or failure.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="errorCode">The error code if the operation failed.</param>
    /// <param name="errorMessages">The error messages if the operation failed.</param>
    protected Result(bool isSuccess, ResultErrorCodes? errorCode = null, (string Context, string Message)[]? errorMessages = null)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessages = errorMessages;
    }

    /// <summary>
    /// Gets the error code if the operation failed.
    /// </summary>
    public ResultErrorCodes? ErrorCode { get; }

    /// <summary>
    /// Gets the error messages if the operation failed.
    /// </summary>
    public (string Context, string Message)[]? ErrorMessages { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="messages">The error messages.</param>
    /// <returns>A failure result.</returns>
    public static Result Failure(ResultErrorCodes code, params (string Context, string Message)[] messages) => new(false, code, messages);

    /// <summary>
    /// Creates a success result.
    /// </summary>
    /// <returns>A success result.</returns>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a success result with a value using type inference.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A success result with a value.</returns>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
}
