using Mentoory.Authorization.Infrastructure.Persistence;
using Mentoory.Identity.Application.Queries.ListUsers.Abstractions;
using Mentoory.Identity.Domain.Aggregates.User;
using Mentoory.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Web.Infrastructure.QueryContexts;

/// <summary>
/// Provides read-only queryable access to identity entities using the IdentityDbContext.
/// </summary>
public class IdentityQueryContext(
    IdentityDbContext dbContext,
    AuthorizationDbContext authorizationDbContext) : IIdentityQueryContext
{
    /// <inheritdoc />
    public IQueryable<User> UsersQueryable() => dbContext.Users.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<long> ActiveUserIdsByIncubatorQueryable(long incubatorId) =>
        authorizationDbContext.RoleAssignments
            .AsNoTracking()
            .Where(ra => ra.IncubatorId == incubatorId && ra.IsActive)
            .Select(ra => ra.UserId)
            .Distinct();
}
