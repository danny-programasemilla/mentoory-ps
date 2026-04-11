using Mentoory.Access.Domain.Aggregates.AuthSession;

namespace Mentoory.Access.Application.Commands.LoginUser;

public sealed record LoginUserResult(
    AuthSession Session,
    bool RequiresPasswordChange);
