namespace Mentoory.Tenant.Application.Configuration;

public interface ISystemConfigurationReader
{
    Task<int> GetIntAsync(string key, CancellationToken cancellationToken);
    Task<bool> GetBoolAsync(string key, CancellationToken cancellationToken);
}
