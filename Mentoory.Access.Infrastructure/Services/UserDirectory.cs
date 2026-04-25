using Mentoory.Access.Application.Users;
using Mentoory.Access.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Access.Infrastructure.Services;

public sealed class UserDirectory(AccessDbContext dbContext) : IUserDirectory
{
    public async Task<IReadOnlyDictionary<long, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<long> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds is null || userIds.Count == 0)
        {
            return new Dictionary<long, string>();
        }

        var ids = userIds.Distinct().ToArray();
        var rows = await dbContext.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, Email = u.Email.Value })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => r.Id,
            r => string.IsNullOrWhiteSpace(r.FirstName) && string.IsNullOrWhiteSpace(r.LastName)
                ? r.Email
                : $"{r.FirstName} {r.LastName}".Trim());
    }
}
