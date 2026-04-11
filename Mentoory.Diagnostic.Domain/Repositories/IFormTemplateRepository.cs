using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Repositories;

public interface IFormTemplateRepository : IRepository<FormTemplate>
{
    FormTemplate Add(FormTemplate formTemplate);
    void Update(FormTemplate formTemplate);
    Task<FormTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<FormTemplate?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    Task<FormTemplate?> GetByIdWithQuestionsAsync(long id, CancellationToken cancellationToken);
    Task<FormTemplate?> GetByExternalIdWithQuestionsAsync(Guid externalId, CancellationToken cancellationToken);
    IQueryable<FormTemplate> Query();
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task<int> CountAsync(IQueryable<FormTemplate> query, CancellationToken cancellationToken);
    Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken);
}
