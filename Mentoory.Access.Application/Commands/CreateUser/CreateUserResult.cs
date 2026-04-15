namespace Mentoory.Access.Application.Commands.CreateUser;

public sealed record CreateUserResult(
    Guid UserExternalId,
    CreateUserOutcome Outcome,
    string? TemporaryPassword,
    IReadOnlyList<string> Warnings);
