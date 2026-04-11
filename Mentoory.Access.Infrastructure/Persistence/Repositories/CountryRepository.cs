using Mentoory.Access.Domain.Aggregates.Country;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Access.Infrastructure.Persistence.Repositories;

public class CountryRepository : AbstractRepository<Country>, ICountryRepository
{
    private readonly AccessDbContext _dbContext;

    public CountryRepository(AccessDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Country>> GetAllActiveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Countries
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Country?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return _dbContext.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }
}
