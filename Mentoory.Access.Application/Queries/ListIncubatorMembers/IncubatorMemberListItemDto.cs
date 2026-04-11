namespace Mentoory.Access.Application.Queries.ListIncubatorMembers;

/// <summary>
/// DTO representing a user who is a member of an incubator, for DataTable display.
/// </summary>
/// <param name="ExternalId">The external GUID identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="AccountStatus">The user's account status display text.</param>
/// <param name="CreatedAtUtc">The UTC timestamp when the user was created.</param>
public sealed record IncubatorMemberListItemDto(
    Guid ExternalId,
    string Email,
    string FirstName,
    string LastName,
    string AccountStatus,
    DateTime CreatedAtUtc);
