using Mentoory.Shared.Application.Queries.Audit;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Shared.Infrastructure.Persistence.Audit;

/// <summary>
/// EF Core implementation of <see cref="IAuditLogReadRepository"/>.
/// Returns a non-tracking <see cref="IQueryable{T}"/> so the handler can compose
/// filters and sorting before the final projection to DTO.
/// </summary>
public sealed class AuditLogReadRepository : IAuditLogReadRepository
{
    private readonly AuditReadDbContext _context;

    public AuditLogReadRepository(AuditReadDbContext context)
    {
        _context = context;
    }

    public IQueryable<AuditLogReadEntity> Query() => _context.AuditLogs.AsNoTracking();
}
