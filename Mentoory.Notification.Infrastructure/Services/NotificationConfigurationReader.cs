using Mentoory.Notification.Contracts.Configuration;
using Mentoory.Notification.Domain.Repositories;

namespace Mentoory.Notification.Infrastructure.Services;

public class NotificationConfigurationReader : INotificationConfigurationReader
{
    private readonly INotificationConfigurationRepository _repository;

    public NotificationConfigurationReader(INotificationConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> GetStringAsync(string key, CancellationToken cancellationToken)
    {
        var config = await _repository.GetByKeyAsync(key, cancellationToken)
                     ?? throw new InvalidOperationException($"Configuration key '{key}' not found.");

        return config.Value;
    }

    public async Task<int> GetIntAsync(string key, CancellationToken cancellationToken)
    {
        var config = await _repository.GetByKeyAsync(key, cancellationToken)
                     ?? throw new InvalidOperationException($"Configuration key '{key}' not found.");

        if (!int.TryParse(config.Value, out var result))
        {
            throw new InvalidOperationException($"Configuration key '{key}' value '{config.Value}' is not a valid integer.");
        }

        return result;
    }

    public async Task<bool> GetBoolAsync(string key, CancellationToken cancellationToken)
    {
        var config = await _repository.GetByKeyAsync(key, cancellationToken)
                     ?? throw new InvalidOperationException($"Configuration key '{key}' not found.");

        if (!bool.TryParse(config.Value, out var result))
        {
            throw new InvalidOperationException($"Configuration key '{key}' value '{config.Value}' is not a valid boolean.");
        }

        return result;
    }
}
