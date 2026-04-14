namespace Mentoory.Notification.Application.Configuration;

public class NotificationSettings
{
    public int PollingIntervalSeconds { get; set; } = 15;

    public int MaxRetryAttempts { get; set; } = 5;

    public string BaseUrl { get; set; } = string.Empty;
}
