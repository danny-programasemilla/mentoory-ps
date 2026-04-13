using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string Country,
    string Identification,
    string FirstName,
    string LastName,
    bool SkipEmailVerification,
    bool SkipInvitationAcceptance,
    Guid ProjectExternalId,
    long CreatedByUserId) : IBaseRequest<CreateUserResult>;
