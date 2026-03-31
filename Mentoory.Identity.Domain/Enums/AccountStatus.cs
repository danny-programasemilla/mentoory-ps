namespace Mentoory.Identity.Domain.Enums;

public enum AccountStatus
{
    PendingVerification = 0,
    Active = 1,
    Locked = 2,
    Disabled = 3,
    PasswordResetRequired = 4,
}
