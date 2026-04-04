namespace Mentoory.Access.Domain.ReadModels;

/// <summary>
/// Read model that mirrors basic user profile data.
/// Created directly during user registration within the Access bounded context.
/// </summary>
public class UserProfile
{
    private UserProfile()
    {
    }

    /// <summary>
    /// Gets the unique identifier for this read model entry.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Gets the Identity domain's internal user ID.
    /// </summary>
    public long UserId { get; private set; }

    /// <summary>
    /// Gets the Identity domain's external GUID for the user.
    /// </summary>
    public Guid UserExternalId { get; private set; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public string Email { get; private set; } = null!;

    /// <summary>
    /// Gets the user's first name.
    /// </summary>
    public string FirstName { get; private set; } = null!;

    /// <summary>
    /// Gets the user's last name.
    /// </summary>
    public string LastName { get; private set; } = null!;

    /// <summary>
    /// Gets the user's account status as a string (e.g., "PendingVerification", "Active").
    /// </summary>
    public string AccountStatus { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC timestamp when the user was originally created in Identity.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this read model was last synchronized.
    /// </summary>
    public DateTime LastSyncedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new UserProfile read model entry.
    /// </summary>
    public static UserProfile Create(
        long userId,
        Guid userExternalId,
        string email,
        string firstName,
        string lastName,
        string accountStatus,
        DateTime createdAtUtc,
        DateTime syncedAtUtc)
    {
        return new UserProfile
        {
            UserId = userId,
            UserExternalId = userExternalId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            AccountStatus = accountStatus,
            CreatedAtUtc = createdAtUtc,
            LastSyncedAtUtc = syncedAtUtc,
        };
    }

    /// <summary>
    /// Updates this read model with the latest data from the Identity domain.
    /// </summary>
    public void UpdateFrom(
        string email,
        string firstName,
        string lastName,
        string accountStatus,
        DateTime syncedAtUtc)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        AccountStatus = accountStatus;
        LastSyncedAtUtc = syncedAtUtc;
    }
}
