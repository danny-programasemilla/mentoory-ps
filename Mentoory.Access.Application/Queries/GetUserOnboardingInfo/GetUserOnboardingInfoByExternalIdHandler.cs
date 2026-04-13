using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetUserOnboardingInfo;

public class GetUserOnboardingInfoByExternalIdHandler(IUserRepository userRepository)
    : BaseCommandHandler<GetUserOnboardingInfoByExternalIdQuery, UserOnboardingInfoDto?>
{
    public override async Task<Result<UserOnboardingInfoDto?>> Handle(
        GetUserOnboardingInfoByExternalIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(request.UserExternalId, cancellationToken);
        if (user is null)
        {
            return Success(null);
        }

        var needsPassword = user.GetActiveCredential() is null
                            || user.AccountStatus == AccountStatus.PasswordResetRequired;

        return Success(new UserOnboardingInfoDto(user.Id, user.ExternalId, needsPassword));
    }
}
