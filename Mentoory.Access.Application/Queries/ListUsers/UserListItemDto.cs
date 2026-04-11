namespace Mentoory.Access.Application.Queries.ListUsers;

/// <summary>
/// Data transfer object representing a user in the list view.
/// </summary>
/// <param name="ExternalId">The external GUID identifier for routing.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="AccountStatus">The user's account status display name.</param>
/// <param name="CreatedAtUtc">The account creation timestamp.</param>
public sealed record UserListItemDto(
    Guid ExternalId,
    string Email,
    string FirstName,
    string LastName,
    string AccountStatus,
    DateTime CreatedAtUtc);
