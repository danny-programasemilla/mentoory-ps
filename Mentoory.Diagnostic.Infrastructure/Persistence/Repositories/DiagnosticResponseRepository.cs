using Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Diagnostic.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing DiagnosticResponse aggregate roots.
/// </summary>
public class DiagnosticResponseRepository : AbstractRepository<DiagnosticResponse>, IDiagnosticResponseRepository
{
    private readonly DiagnosticDbContext _dbContext;

    public DiagnosticResponseRepository(DiagnosticDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public new DiagnosticResponse Add(DiagnosticResponse diagnosticResponse)
    {
        return _dbContext.DiagnosticResponses.Add(diagnosticResponse).Entity;
    }

    /// <inheritdoc />
    public new void Update(DiagnosticResponse diagnosticResponse)
    {
        _dbContext.Entry(diagnosticResponse).State = EntityState.Modified;
    }

    /// <inheritdoc />
    public Task<DiagnosticResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.DiagnosticResponses
            .Include(r => r.QuestionResponses)
                .ThenInclude(qr => qr.Corrections)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DiagnosticResponse?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.DiagnosticResponses
            .FirstOrDefaultAsync(r => r.ExternalId == externalId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DiagnosticResponse?> GetByExternalIdAsync(Guid externalId, long projectId, CancellationToken cancellationToken)
    {
        return _dbContext.DiagnosticResponses
            .FirstOrDefaultAsync(r => r.ExternalId == externalId && r.ProjectId == projectId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DiagnosticResponse?> GetByExternalIdWithResponsesAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.DiagnosticResponses
            .Include(r => r.QuestionResponses)
                .ThenInclude(qr => qr.Corrections)
            .FirstOrDefaultAsync(r => r.ExternalId == externalId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DiagnosticResponse?> GetByExternalIdWithResponsesAsync(Guid externalId, long projectId, CancellationToken cancellationToken)
    {
        return _dbContext.DiagnosticResponses
            .Include(r => r.QuestionResponses)
                .ThenInclude(qr => qr.Corrections)
            .FirstOrDefaultAsync(r => r.ExternalId == externalId && r.ProjectId == projectId, cancellationToken);
    }

    /// <inheritdoc />
    public IQueryable<DiagnosticResponse> Query()
    {
        return _dbContext.DiagnosticResponses.AsNoTracking();
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return _dbContext.DiagnosticResponses.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(IQueryable<DiagnosticResponse> query, CancellationToken cancellationToken)
    {
        return query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }
}
