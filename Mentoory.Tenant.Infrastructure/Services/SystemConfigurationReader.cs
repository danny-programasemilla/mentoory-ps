using Mentoory.Tenant.Application.Configuration;
using Mentoory.Tenant.Domain.Repositories;

namespace Mentoory.Tenant.Infrastructure.Services;

public class SystemConfigurationReader : ISystemConfigurationReader
{
    private readonly ISystemConfigurationRepository _repository;

    public SystemConfigurationReader(ISystemConfigurationRepository repository)
    {
        _repository = repository;
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
