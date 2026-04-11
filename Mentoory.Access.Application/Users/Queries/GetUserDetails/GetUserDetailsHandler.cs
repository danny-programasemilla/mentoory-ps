using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Users.Queries.GetUserDetails;

/// <summary>
/// Handles the GetUserDetailsQuery by loading the User aggregate from the Access domain.
/// </summary>
public class GetUserDetailsHandler(IUserRepository userRepository)
    : BaseCommandHandler<GetUserDetailsQuery, UserDetailsDto>
{
    /// <inheritdoc />
    public override async Task<Result<UserDetailsDto>> Handle(
        GetUserDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(request.UserExternalId, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("GetUserDetails", "Usuario no encontrado."));
        }

        var activeCredentialsCount = user.Credentials.Count(c => c.IsActive);

        var dto = new UserDetailsDto(
            user.ExternalId,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.AccountStatus.ToString(),
            user.EmailVerifiedAtUtc,
            activeCredentialsCount,
            user.CreatedAtUtc);

        return Success(dto);
    }
}
