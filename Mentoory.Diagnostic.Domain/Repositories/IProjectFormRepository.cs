using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Repositories;

public interface IProjectFormRepository : IRepository<ProjectForm>
{
    ProjectForm Add(ProjectForm projectForm);
    void Update(ProjectForm projectForm);
    Task<ProjectForm?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<ProjectForm?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    Task<ProjectForm?> GetByExternalIdAsync(Guid externalId, long projectId, CancellationToken cancellationToken);
    Task<ProjectForm?> GetByIdWithQuestionsAsync(long id, CancellationToken cancellationToken);
    Task<ProjectForm?> GetByExternalIdWithQuestionsAsync(Guid externalId, CancellationToken cancellationToken);
    Task<ProjectForm?> GetByExternalIdWithQuestionsAsync(Guid externalId, long projectId, CancellationToken cancellationToken);
    IQueryable<ProjectForm> Query();
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task<int> CountAsync(IQueryable<ProjectForm> query, CancellationToken cancellationToken);
    Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken);
}
