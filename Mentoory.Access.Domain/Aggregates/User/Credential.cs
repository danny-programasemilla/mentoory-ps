using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Access.Domain.Aggregates.User;

public class Credential : Entity
{
    private Credential()
    {
    }

    public string PasswordHash { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Credential Create(string passwordHash, DateTime utcNow)
    {
        return new Credential
        {
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAtUtc = utcNow,
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
