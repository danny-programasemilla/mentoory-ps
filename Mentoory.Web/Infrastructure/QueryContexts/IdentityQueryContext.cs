using Mentoory.Identity.Application.Queries.ListUsers.Abstractions;
using Mentoory.Identity.Domain.Aggregates.User;
using Mentoory.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Web.Infrastructure.QueryContexts;

/// <summary>
/// Provides read-only queryable access to identity entities using the IdentityDbContext.
/// </summary>
public class IdentityQueryContext(IdentityDbContext dbContext) : IIdentityQueryContext
{
    /// <inheritdoc />
    public IQueryable<User> UsersQueryable() => dbContext.Users.AsNoTracking();
}
