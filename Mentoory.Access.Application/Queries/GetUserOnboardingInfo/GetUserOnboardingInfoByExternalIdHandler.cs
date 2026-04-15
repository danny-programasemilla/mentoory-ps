using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetUserOnboardingInfo;

/// <summary>
/// Handles the query to retrieve onboarding information for a user by their external identifier.
/// </summary>
public class GetUserOnboardingInfoByExternalIdHandler
    : BaseCommandHandler<GetUserOnboardingInfoByExternalIdQuery, UserOnboardingInfoDto?>
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserOnboardingInfoByExternalIdHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for data access.</param>
    public GetUserOnboardingInfoByExternalIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public override async Task<Result<UserOnboardingInfoDto?>> Handle(
        GetUserOnboardingInfoByExternalIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(request.UserExternalId, cancellationToken);
        if (user is null)
        {
            return Success(null);
        }

        var needsPassword = user.GetActiveCredential() is null
                            || user.AccountStatus == AccountStatus.PasswordResetRequired;

        return Success(new UserOnboardingInfoDto(user.Id, user.ExternalId, needsPassword));
    }
}
