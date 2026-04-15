namespace Mentoory.Notification.Contracts.Configuration;

public interface INotificationConfigurationReader
{
    Task<string> GetStringAsync(string key, CancellationToken cancellationToken);
    Task<int> GetIntAsync(string key, CancellationToken cancellationToken);
    Task<bool> GetBoolAsync(string key, CancellationToken cancellationToken);
}
