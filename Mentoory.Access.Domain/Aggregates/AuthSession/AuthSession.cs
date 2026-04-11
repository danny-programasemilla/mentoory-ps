using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Access.Domain.Aggregates.AuthSession;

public class AuthSession : Entity, IAggregateRoot
{
    private AuthSession()
    {
    }

    public string SessionToken { get; private set; } = null!;
    public long UserId { get; private set; }
    public string IpAddress { get; private set; } = null!;
    public string? UserAgent { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime LastActivityUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public long? ActiveIncubatorId { get; private set; }
    public long? ActiveProjectId { get; private set; }
    public string? ActiveRole { get; private set; }

    public static AuthSession Create(
        string sessionToken,
        long userId,
        string ipAddress,
        string? userAgent,
        DateTime utcNow,
        TimeSpan absoluteTimeout)
    {
        return new AuthSession
        {
            SessionToken = sessionToken,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAtUtc = utcNow,
            LastActivityUtc = utcNow,
            ExpiresAtUtc = utcNow.Add(absoluteTimeout),
            IsActive = true,
        };
    }

    public bool IsValid(DateTime utcNow)
    {
        return IsActive && utcNow < ExpiresAtUtc;
    }

    public void UpdateActivity(DateTime utcNow)
    {
        LastActivityUtc = utcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void SetContext(long incubatorId, long? projectId, string role)
    {
        ActiveIncubatorId = incubatorId;
        ActiveProjectId = projectId;
        ActiveRole = role;
    }

    public void ClearContext()
    {
        ActiveIncubatorId = null;
        ActiveProjectId = null;
        ActiveRole = null;
    }
}
