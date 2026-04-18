namespace Mentoory.Access.Application.Users;

/// <summary>
/// Lightweight lookup for rendering user-facing display names from internal user ids.
/// Kept as a focused interface (single method) to avoid coupling other modules to
/// the full user aggregate or the repository abstraction.
/// </summary>
public interface IUserDirectory
{
    /// <summary>
    /// Returns a map of user id to display name for every user found in the input set.
    /// Users that do not exist are simply absent from the returned dictionary.
    /// </summary>
    Task<IReadOnlyDictionary<long, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<long> userIds,
        CancellationToken cancellationToken);
}
