using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.ValueObjects;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Access.Domain.Aggregates.User;

public class User : Entity, IAggregateRoot
{
    private readonly List<Credential> _credentials = new();
    private readonly List<EmailVerificationToken> _emailVerificationTokens = new();
    private readonly List<PasswordResetToken> _passwordResetTokens = new();

    private User()
    {
    }

    public Guid ExternalId { get; private set; }
    public EmailAddress Email { get; private set; } = null!;
    public NationalIdentity NationalIdentity { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public AccountStatus AccountStatus { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEndUtc { get; private set; }
    public DateTime? EmailVerifiedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<Credential> Credentials => _credentials.AsReadOnly();
    public IReadOnlyCollection<EmailVerificationToken> EmailVerificationTokens => _emailVerificationTokens.AsReadOnly();
    public IReadOnlyCollection<PasswordResetToken> PasswordResetTokens => _passwordResetTokens.AsReadOnly();

    public static User Register(
        string email,
        string country,
        string nationalId,
        string firstName,
        string lastName,
        string passwordHash,
        DateTime utcNow)
    {
        var user = new User
        {
            ExternalId = Guid.NewGuid(),
            Email = new EmailAddress(email),
            NationalIdentity = new NationalIdentity(country, nationalId),
            FirstName = firstName,
            LastName = lastName,
            AccountStatus = AccountStatus.PendingVerification,
            FailedLoginAttempts = 0,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
        };

        user._credentials.Add(Credential.Create(passwordHash, utcNow));
        return user;
    }

    public void VerifyEmail(DateTime utcNow)
    {
        if (AccountStatus != AccountStatus.PendingVerification)
        {
            throw new InvalidOperationException("Only pending verification accounts can be verified.");
        }

        AccountStatus = AccountStatus.Active;
        EmailVerifiedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void RecordFailedLogin(DateTime utcNow, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        UpdatedAtUtc = utcNow;

        if (FailedLoginAttempts >= maxAttempts)
        {
            Lock(utcNow, lockoutDuration);
        }
    }

    public void RecordSuccessfulLogin(DateTime utcNow)
    {
        FailedLoginAttempts = 0;
        UpdatedAtUtc = utcNow;
    }

    public void Lock(DateTime utcNow, TimeSpan duration)
    {
        if (AccountStatus == AccountStatus.Locked)
        {
            throw new InvalidOperationException("Account is already locked.");
        }

        AccountStatus = AccountStatus.Locked;
        LockoutEndUtc = utcNow.Add(duration);
        UpdatedAtUtc = utcNow;
    }

    public void Unlock(DateTime utcNow)
    {
        AccountStatus = AccountStatus.Active;
        LockoutEndUtc = null;
        FailedLoginAttempts = 0;
        UpdatedAtUtc = utcNow;
    }

    public bool IsLockedOut(DateTime utcNow)
    {
        if (AccountStatus != AccountStatus.Locked)
        {
            return false;
        }

        if (LockoutEndUtc.HasValue && utcNow >= LockoutEndUtc.Value)
        {
            // Lockout expired, auto-unlock
            Unlock(utcNow);
            return false;
        }

        return true;
    }

    public void Activate(DateTime utcNow)
    {
        AccountStatus = AccountStatus.Active;
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTime utcNow)
    {
        AccountStatus = AccountStatus.Disabled;
        UpdatedAtUtc = utcNow;
    }

    public void ChangePassword(string newPasswordHash, DateTime utcNow)
    {
        // Deactivate current credential
        var active = _credentials.SingleOrDefault(c => c.IsActive);
        active?.Deactivate();

        _credentials.Add(Credential.Create(newPasswordHash, utcNow));
        UpdatedAtUtc = utcNow;

        if (AccountStatus == AccountStatus.PasswordResetRequired)
        {
            AccountStatus = AccountStatus.Active;
        }
    }

    public bool HasUsedPassword(string passwordHash, int historyDepth)
    {
        return _credentials
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(historyDepth)
            .Any(c => c.PasswordHash == passwordHash);
    }

    public Credential? GetActiveCredential()
    {
        return _credentials.SingleOrDefault(c => c.IsActive);
    }

    public EmailVerificationToken GenerateEmailVerificationToken(DateTime utcNow, string tokenHash, int expiryHours = 24)
    {
        // Invalidate existing unused/unexpired tokens
        foreach (var token in _emailVerificationTokens.Where(t => !t.IsUsed))
        {
            token.MarkAsUsed();
        }

        var newToken = EmailVerificationToken.Create(tokenHash, utcNow, utcNow.AddHours(expiryHours));
        _emailVerificationTokens.Add(newToken);
        return newToken;
    }

    public PasswordResetToken GeneratePasswordResetToken(DateTime utcNow, string tokenHash, int expiryHours = 1)
    {
        // Invalidate existing unused tokens
        foreach (var token in _passwordResetTokens.Where(t => !t.IsUsed))
        {
            token.MarkAsUsed();
        }

        var newToken = PasswordResetToken.Create(tokenHash, utcNow, utcNow.AddHours(expiryHours));
        _passwordResetTokens.Add(newToken);
        return newToken;
    }

    public void AdminVerifyEmail(DateTime utcNow)
    {
        if (AccountStatus != AccountStatus.PendingVerification)
        {
            throw new InvalidOperationException("Only pending verification accounts can be verified.");
        }

        AccountStatus = AccountStatus.Active;
        EmailVerifiedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void SetPasswordResetRequired(DateTime utcNow)
    {
        AccountStatus = AccountStatus.PasswordResetRequired;
        UpdatedAtUtc = utcNow;
    }
}
