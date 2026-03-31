using Mentoory.Identity.Domain.Aggregates.User;

namespace Mentoory.Identity.Application.Queries.ListUsers.Abstractions;

/// <summary>
/// Provides read-only queryable access to identity entities for query handlers.
/// </summary>
public interface IIdentityQueryContext
{
    /// <summary>
    /// Gets a no-tracking queryable for User entities.
    /// </summary>
    /// <returns>An IQueryable of User for read-only queries.</returns>
    IQueryable<User> UsersQueryable();
}
