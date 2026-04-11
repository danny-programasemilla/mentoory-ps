using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.LoginUser;

public sealed record LoginUserCommand(
    string Email,
    string Password,
    string IpAddress,
    string? UserAgent) : IBaseRequest<LoginUserResult>;
