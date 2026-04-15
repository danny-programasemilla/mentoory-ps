using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.ValidateVerificationToken;

/// <summary>
/// Query to validate a verification token for a user.
/// </summary>
/// <param name="UserExternalId">The external identifier of the user.</param>
/// <param name="Token">The raw verification token to validate.</param>
public sealed record ValidateVerificationTokenQuery(Guid UserExternalId, string Token)
    : IBaseRequest<VerificationTokenValidationDto?>;

/// <summary>
/// Result of a verification token validation.
/// </summary>
/// <param name="UserExternalId">The external identifier of the validated user.</param>
public sealed record VerificationTokenValidationDto(Guid UserExternalId);
