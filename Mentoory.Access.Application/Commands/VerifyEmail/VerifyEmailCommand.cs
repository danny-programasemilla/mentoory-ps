using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.VerifyEmail;

/// <summary>
/// Represents a command to verify a user's email address using a verification token.
/// </summary>
/// <param name="TokenHash">The hashed verification token.</param>
public sealed record VerifyEmailCommand(string TokenHash) : IBaseRequest;
