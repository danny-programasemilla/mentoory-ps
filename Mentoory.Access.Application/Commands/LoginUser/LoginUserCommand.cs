using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.LoginUser;

[Audited(AuditEventTypes.UserLoggedIn, EntityType = "User")]
public sealed record LoginUserCommand(
    string Email,
    string Password,
    string IpAddress,
    string? UserAgent) : IBaseRequest<LoginUserResult>;
