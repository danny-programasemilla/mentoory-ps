using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Access.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing User aggregate roots.
/// </summary>
public class UserRepository : AbstractRepository<User>, IUserRepository
{
    private readonly AccessDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The Identity database context.</param>
    public UserRepository(AccessDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public new User Add(User user)
    {
        return _dbContext.Users.Add(user).Entity;
    }

    /// <inheritdoc />
    public new void Update(User user)
    {
        _dbContext.Entry(user).State = EntityState.Modified;
    }

    /// <inheritdoc />
    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(u => u.Credentials)
            .Include(u => u.EmailVerificationTokens)
            .Include(u => u.PasswordResetTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(u => u.Credentials)
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(u => u.Credentials)
            .FirstOrDefaultAsync(u => u.Email.NormalizedValue == normalizedEmail, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByNationalIdentityAsync(string country, string nationalId, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(u => u.Credentials)
            .FirstOrDefaultAsync(u => u.NationalIdentity.Country == country && u.NationalIdentity.NationalId == nationalId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .AnyAsync(u => u.Email.NormalizedValue == normalizedEmail, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNationalIdentityAsync(string country, string nationalId, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .AnyAsync(u => u.NationalIdentity.Country == country && u.NationalIdentity.NationalId == nationalId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByEmailVerificationTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(u => u.EmailVerificationTokens)
            .FirstOrDefaultAsync(u => u.EmailVerificationTokens.Any(t => t.TokenHash == tokenHash), cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(u => u.Credentials)
            .Include(u => u.PasswordResetTokens)
            .FirstOrDefaultAsync(u => u.PasswordResetTokens.Any(t => t.TokenHash == tokenHash), cancellationToken);
    }

    /// <inheritdoc />
    public IQueryable<User> Query()
    {
        return _dbContext.Users.AsNoTracking();
    }
}
