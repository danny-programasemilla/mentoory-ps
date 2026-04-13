using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.SetInitialPassword;

public sealed record SetInitialPasswordCommand(
    Guid UserExternalId,
    string Token,
    string NewPassword,
    string ConfirmPassword) : IBaseRequest;
