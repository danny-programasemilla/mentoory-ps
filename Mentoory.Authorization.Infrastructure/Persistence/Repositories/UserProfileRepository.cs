using Mentoory.Authorization.Domain.ReadModels;
using Mentoory.Authorization.Domain.Repositories;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Authorization.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for the UserProfile read model.
/// </summary>
/// <param name="dbContext">The database context for accessing authorization data.</param>
public class UserProfileRepository(AuthorizationDbContext dbContext) : IUserProfileRepository
{
    /// <inheritdoc />
    public IUnitOfWork UnitOfWork => dbContext;

    /// <inheritdoc />
    public Task<UserProfile?> GetByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        return dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public void Add(UserProfile profile)
    {
        dbContext.UserProfiles.Add(profile);
    }

    /// <inheritdoc />
    public void Update(UserProfile profile)
    {
        if (dbContext.Entry(profile).State == EntityState.Detached)
        {
            dbContext.UserProfiles.Update(profile);
        }
    }

    /// <inheritdoc />
    public IQueryable<UserProfile> Query()
    {
        return dbContext.UserProfiles.AsNoTracking();
    }
}
