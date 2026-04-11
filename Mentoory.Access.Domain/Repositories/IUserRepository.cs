using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Access.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    User Add(User user);
    void Update(User user);
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<User?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User?> GetByNationalIdentityAsync(string country, string nationalId, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> ExistsByNationalIdentityAsync(string country, string nationalId, CancellationToken cancellationToken);
    Task<User?> GetByEmailVerificationTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task<User?> GetByPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a read-only queryable for users.
    /// </summary>
    IQueryable<User> Query();
}
