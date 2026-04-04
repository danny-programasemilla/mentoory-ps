using Mentoory.Access.Domain.ReadModels;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Access.Domain.Repositories;

/// <summary>
/// Repository interface for the UserProfile read model.
/// Provides persistence operations for user profile data synchronized from the Identity domain.
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>
    /// Gets the unit of work associated with the repository.
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Retrieves a user profile by the Identity domain's user ID.
    /// </summary>
    /// <param name="userId">The Identity domain's internal user ID.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The user profile if found; otherwise, null.</returns>
    Task<UserProfile?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user profile to the repository.
    /// </summary>
    /// <param name="profile">The user profile to add.</param>
    void Add(UserProfile profile);

    /// <summary>
    /// Updates an existing user profile in the repository.
    /// </summary>
    /// <param name="profile">The user profile to update.</param>
    void Update(UserProfile profile);

    /// <summary>
    /// Gets a read-only queryable for user profiles.
    /// </summary>
    IQueryable<UserProfile> Query();
}
