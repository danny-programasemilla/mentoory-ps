using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.RegisterInternalUser;

public sealed record RegisterInternalUserCommand(
    string Country,
    string Identification,
    string Email,
    string Password,
    bool RequireEmailVerification,
    Guid ProjectExternalId) : IBaseRequest<RegisterInternalUserResult>;
