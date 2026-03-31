using Mentoory.Identity.Domain.Aggregates.AuthSession;
using Mentoory.Identity.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Identity.Infrastructure.Persistence.Repositories;

public class AuthSessionRepository : AbstractRepository<AuthSession>, IAuthSessionRepository
{
    private readonly IdentityDbContext _dbContext;

    public AuthSessionRepository(IdentityDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new AuthSession Add(AuthSession session)
    {
        return _dbContext.AuthSessions.Add(session).Entity;
    }

    public new void Update(AuthSession session)
    {
        _dbContext.Entry(session).State = EntityState.Modified;
    }

    public Task<AuthSession?> GetByTokenAsync(string sessionToken, CancellationToken cancellationToken)
    {
        return _dbContext.AuthSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive, cancellationToken);
    }

    public Task<List<AuthSession>> GetActiveSessionsByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        return _dbContext.AuthSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync(cancellationToken);
    }
}
