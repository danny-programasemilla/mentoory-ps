using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.RegenerateVerificationToken;

public sealed record RegenerateVerificationTokenCommand(Guid UserExternalId) : IBaseRequest;
