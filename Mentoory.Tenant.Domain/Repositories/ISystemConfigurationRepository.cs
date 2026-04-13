using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Domain.Repositories;

public interface ISystemConfigurationRepository : IRepository<Aggregates.SystemConfiguration.SystemConfiguration>
{
    Task<Aggregates.SystemConfiguration.SystemConfiguration?> GetByKeyAsync(string key, CancellationToken cancellationToken);
    Task<List<Aggregates.SystemConfiguration.SystemConfiguration>> GetAllAsync(CancellationToken cancellationToken);
    void Update(Aggregates.SystemConfiguration.SystemConfiguration configuration);
}
