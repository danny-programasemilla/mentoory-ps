using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;

namespace Mentoory.Access.Application.Queries.ValidateVerificationToken;

/// <summary>
/// Handles the validation of a verification token for a user.
/// </summary>
public class ValidateVerificationTokenHandler
    : BaseCommandHandler<ValidateVerificationTokenQuery, VerificationTokenValidationDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateVerificationTokenHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for data access.</param>
    /// <param name="passwordHasher">The password hasher for verifying token hashes.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    public ValidateVerificationTokenHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public override async Task<Result<VerificationTokenValidationDto?>> Handle(
        ValidateVerificationTokenQuery request,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByExternalIdAsync(request.UserExternalId, cancellationToken);
        if (user is null)
        {
            return Success(null);
        }

        var hasValidToken = HasMatchingValidToken(user, request.Token, utcNow);
        if (!hasValidToken)
        {
            return Success(null);
        }

        return Success(new VerificationTokenValidationDto(user.ExternalId));
    }

    private bool HasMatchingValidToken(
        Domain.Aggregates.User.User user,
        string rawToken,
        DateTime utcNow)
    {
        foreach (var token in user.EmailVerificationTokens)
        {
            if (!token.IsUsed && token.IsValid(utcNow) && _passwordHasher.VerifyPassword(rawToken, token.TokenHash))
            {
                return true;
            }
        }

        return false;
    }
}
