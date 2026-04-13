using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.ValidateVerificationToken;

public sealed record ValidateVerificationTokenQuery(Guid UserExternalId, string Token)
    : IBaseRequest<VerificationTokenValidationDto?>;

public sealed record VerificationTokenValidationDto(Guid UserExternalId);
