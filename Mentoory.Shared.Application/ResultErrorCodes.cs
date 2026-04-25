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

    /// <summary>
    /// A project with the given ExternalId does not exist (project lifecycle).
    /// </summary>
    ProjectNotFound = 100_100,

    /// <summary>
    /// The acting user's incubator context does not contain the target project (project lifecycle).
    /// </summary>
    ProjectOutOfScope = 100_101,

    /// <summary>
    /// The project is inactive; lifecycle operations are refused.
    /// </summary>
    ProjectInactive = 100_102,

    /// <summary>
    /// The project's current stage is not in progress; advancement is refused.
    /// </summary>
    StageNotInProgress = 100_103,

    /// <summary>
    /// The project is already at its final (Closure) stage; no further advancement is possible.
    /// </summary>
    ProjectAlreadyClosed = 100_104,

    /// <summary>
    /// A concurrent modification of the project's lifecycle was detected (row version mismatch).
    /// </summary>
    LifecycleConcurrencyConflict = 100_105,
}
