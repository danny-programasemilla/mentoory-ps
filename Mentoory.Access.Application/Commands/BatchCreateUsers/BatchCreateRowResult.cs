using Mentoory.Access.Application.Commands.CreateUser;

namespace Mentoory.Access.Application.Commands.BatchCreateUsers;

public sealed record BatchCreateRowResult
{
    public int RowNumber { get; init; }
    public string Country { get; init; } = string.Empty;
    public string Identification { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public CreateUserOutcome? Outcome { get; init; }
    public string? TemporaryPassword { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> Errors { get; init; } = [];
}
