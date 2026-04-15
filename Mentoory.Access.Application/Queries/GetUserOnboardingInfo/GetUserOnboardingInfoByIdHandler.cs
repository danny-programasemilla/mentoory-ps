using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetUserOnboardingInfo;

/// <summary>
/// Handles the query to retrieve onboarding information for a user by their internal identifier.
/// </summary>
public class GetUserOnboardingInfoByIdHandler
    : BaseCommandHandler<GetUserOnboardingInfoByIdQuery, UserOnboardingInfoDto?>
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserOnboardingInfoByIdHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for data access.</param>
    public GetUserOnboardingInfoByIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public override async Task<Result<UserOnboardingInfoDto?>> Handle(
        GetUserOnboardingInfoByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Success(null);
        }

        var needsPassword = user.GetActiveCredential() is null
                            || user.AccountStatus == AccountStatus.PasswordResetRequired;

        return Success(new UserOnboardingInfoDto(user.Id, user.ExternalId, needsPassword));
    }
}
