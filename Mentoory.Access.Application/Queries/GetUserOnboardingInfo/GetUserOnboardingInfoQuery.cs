using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetUserOnboardingInfo;

/// <summary>
/// Query to retrieve onboarding information for a user by their internal identifier.
/// </summary>
/// <param name="UserId">The internal identifier of the user.</param>
public sealed record GetUserOnboardingInfoByIdQuery(long UserId) : IBaseRequest<UserOnboardingInfoDto?>;

/// <summary>
/// Query to retrieve onboarding information for a user by their external identifier.
/// </summary>
/// <param name="UserExternalId">The external identifier of the user.</param>
public sealed record GetUserOnboardingInfoByExternalIdQuery(Guid UserExternalId) : IBaseRequest<UserOnboardingInfoDto?>;

/// <summary>
/// Onboarding information for a user.
/// </summary>
/// <param name="UserId">The internal identifier of the user.</param>
/// <param name="UserExternalId">The external identifier of the user.</param>
/// <param name="NeedsPassword">Whether the user needs to set or reset their password.</param>
public sealed record UserOnboardingInfoDto(long UserId, Guid UserExternalId, bool NeedsPassword);
