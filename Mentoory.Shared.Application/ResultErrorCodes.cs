// ReSharper disable InconsistentNaming
namespace Mentoory.Shared.Application;

/// <summary>
/// Enumeration of error codes for result operations.
/// </summary>
public enum ResultErrorCodes
{
    /// <summary>
    /// Indicates an unknown error.
    /// </summary>
    Unknown = 100_000,

    /// <summary>
    /// Indicates a generic error.
    /// </summary>
    GenericError = 100_001,

    /// <summary>
    /// Indicates that one or more fields failed validation checks.
    /// </summary>
    Validation_SomeFieldsAreInvalid = 100_002,
}
