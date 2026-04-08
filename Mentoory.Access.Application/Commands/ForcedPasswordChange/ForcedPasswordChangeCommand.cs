using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.ForcedPasswordChange;

public sealed record ForcedPasswordChangeCommand(
    long UserId,
    string CurrentPassword,
    string NewPassword) : IBaseRequest;
