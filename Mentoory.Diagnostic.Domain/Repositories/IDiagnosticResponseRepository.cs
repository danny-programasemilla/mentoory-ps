using Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Repositories;

public interface IDiagnosticResponseRepository : IRepository<DiagnosticResponse>
{
    DiagnosticResponse Add(DiagnosticResponse diagnosticResponse);
    void Update(DiagnosticResponse diagnosticResponse);
    Task<DiagnosticResponse?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<DiagnosticResponse?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    Task<DiagnosticResponse?> GetByExternalIdAsync(Guid externalId, long projectId, CancellationToken cancellationToken);
    Task<DiagnosticResponse?> GetByExternalIdWithResponsesAsync(Guid externalId, CancellationToken cancellationToken);
    Task<DiagnosticResponse?> GetByExternalIdWithResponsesAsync(Guid externalId, long projectId, CancellationToken cancellationToken);
    IQueryable<DiagnosticResponse> Query();
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task<int> CountAsync(IQueryable<DiagnosticResponse> query, CancellationToken cancellationToken);
    Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken);
}
