using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.AdminVerifyEmail;

public sealed record AdminVerifyEmailCommand(Guid UserExternalId) : IBaseRequest;
