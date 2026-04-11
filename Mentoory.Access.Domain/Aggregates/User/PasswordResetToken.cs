using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Access.Domain.Aggregates.User;

public class PasswordResetToken : Entity
{
    private PasswordResetToken()
    {
    }

    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static PasswordResetToken Create(string tokenHash, DateTime utcNow, DateTime expiresAtUtc)
    {
        return new PasswordResetToken
        {
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            IsUsed = false,
            CreatedAtUtc = utcNow,
        };
    }

    public bool IsValid(DateTime utcNow) => !IsUsed && utcNow < ExpiresAtUtc;

    public void MarkAsUsed() => IsUsed = true;
}
