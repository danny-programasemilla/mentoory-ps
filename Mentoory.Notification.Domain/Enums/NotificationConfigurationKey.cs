namespace Mentoory.Notification.Domain.Enums;

public enum NotificationConfigurationKey
{
    SmtpHost = 0,
    SmtpPort = 1,
    SmtpUsername = 2,
    SmtpPassword = 3,
    SmtpFromAddress = 4,
    SmtpFromName = 5,
    SmtpUseSsl = 6,
    PollingIntervalSeconds = 7,
    MaxRetryAttempts = 8,
    BaseUrl = 9,
}
