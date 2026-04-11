namespace Mentoory.Access.Domain.Enums;

public enum ConfigurationKey
{
    EmailVerificationTokenExpiryHours = 0,
    PasswordResetTokenExpiryHours = 1,
    InvitationTokenExpiryHours = 2,
    MaxFailedLoginAttempts = 3,
    LockoutDurationMinutes = 4,
    SessionTimeoutHours = 5,
    PasswordHistoryDepth = 6,
}
