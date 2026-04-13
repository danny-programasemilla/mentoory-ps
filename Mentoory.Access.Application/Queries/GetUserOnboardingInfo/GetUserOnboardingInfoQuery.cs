using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetUserOnboardingInfo;

public sealed record GetUserOnboardingInfoByIdQuery(long UserId)
    : IBaseRequest<UserOnboardingInfoDto?>;

public sealed record GetUserOnboardingInfoByExternalIdQuery(Guid UserExternalId)
    : IBaseRequest<UserOnboardingInfoDto?>;

public sealed record UserOnboardingInfoDto(
    long UserId,
    Guid UserExternalId,
    bool NeedsPassword);
