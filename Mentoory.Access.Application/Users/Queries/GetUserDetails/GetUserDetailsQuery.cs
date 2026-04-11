using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Users.Queries.GetUserDetails;

/// <summary>
/// Query to retrieve detailed user information from the Access domain.
/// </summary>
/// <param name="UserExternalId">The external GUID identifier of the user.</param>
public sealed record GetUserDetailsQuery(Guid UserExternalId) : IBaseRequest<UserDetailsDto>;

/// <summary>
/// Detailed DTO for a user returned by the GetUserDetails query.
/// </summary>
/// <param name="ExternalId">The external GUID identifier for routing.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="AccountStatus">The user's account status display name.</param>
/// <param name="EmailVerifiedAtUtc">When the email was verified, if applicable.</param>
/// <param name="ActiveCredentialsCount">Number of active credentials for the user.</param>
/// <param name="CreatedAtUtc">The account creation timestamp.</param>
public sealed record UserDetailsDto(
    Guid ExternalId,
    string Email,
    string FirstName,
    string LastName,
    string AccountStatus,
    DateTime? EmailVerifiedAtUtc,
    int ActiveCredentialsCount,
    DateTime CreatedAtUtc);
