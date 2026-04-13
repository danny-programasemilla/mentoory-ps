namespace Mentoory.Access.Application.Commands.BatchRegisterUsers;

public sealed record BatchRowResult
{
    public int RowNumber { get; init; }
    public string Country { get; init; } = string.Empty;
    public string Identification { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool UserAlreadyExisted { get; init; }
    public bool UserCreated { get; init; }
    public bool AlreadyInProject { get; init; }
    public bool InvitationCreated { get; init; }
    public bool EnrolledDirectly { get; init; }
    public string? TemporaryPassword { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
