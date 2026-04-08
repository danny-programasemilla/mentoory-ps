using Mentoory.Access.Domain.Aggregates.Country;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Access.Domain.Repositories;

public interface ICountryRepository : IRepository<Country>
{
    Task<List<Country>> GetAllActiveAsync(CancellationToken cancellationToken);
    Task<Country?> GetByCodeAsync(string code, CancellationToken cancellationToken);
}
