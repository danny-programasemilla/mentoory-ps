using Mentoory.Identity.Domain.Aggregates.AuthSession;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Identity.Domain.Repositories;

public interface IAuthSessionRepository : IRepository<AuthSession>
{
    AuthSession Add(AuthSession session);
    void Update(AuthSession session);
    Task<AuthSession?> GetByTokenAsync(string sessionToken, CancellationToken cancellationToken);
    Task<List<AuthSession>> GetActiveSessionsByUserIdAsync(long userId, CancellationToken cancellationToken);
}
