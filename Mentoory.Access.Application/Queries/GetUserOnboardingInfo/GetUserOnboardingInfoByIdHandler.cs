using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetUserOnboardingInfo;

public class GetUserOnboardingInfoByIdHandler(IUserRepository userRepository)
    : BaseCommandHandler<GetUserOnboardingInfoByIdQuery, UserOnboardingInfoDto?>
{
    public override async Task<Result<UserOnboardingInfoDto?>> Handle(
        GetUserOnboardingInfoByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Success(null);
        }

        var needsPassword = user.GetActiveCredential() is null
                            || user.AccountStatus == AccountStatus.PasswordResetRequired;

        return Success(new UserOnboardingInfoDto(user.Id, user.ExternalId, needsPassword));
    }
}
