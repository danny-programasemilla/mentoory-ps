using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;

namespace Mentoory.Access.Application.Queries.ValidateVerificationToken;

public class ValidateVerificationTokenHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITimeProvider timeProvider)
    : BaseCommandHandler<ValidateVerificationTokenQuery, VerificationTokenValidationDto?>
{
    public override async Task<Result<VerificationTokenValidationDto?>> Handle(
        ValidateVerificationTokenQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(request.UserExternalId, cancellationToken);
        if (user is null)
        {
            return Success(null);
        }

        var token = user.EmailVerificationTokens
            .FirstOrDefault(t => passwordHasher.VerifyPassword(request.Token, t.TokenHash));

        if (token is null || !token.IsValid(timeProvider.UtcNow))
        {
            return Success(null);
        }

        return Success(new VerificationTokenValidationDto(user.ExternalId));
    }
}
