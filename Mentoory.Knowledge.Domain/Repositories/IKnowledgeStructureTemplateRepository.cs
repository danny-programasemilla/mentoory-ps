using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Knowledge.Domain.Repositories;

public interface IKnowledgeStructureTemplateRepository : IRepository<KnowledgeStructureTemplate>
{
    KnowledgeStructureTemplate Add(KnowledgeStructureTemplate template);
    void Update(KnowledgeStructureTemplate template);
    void Remove(KnowledgeStructureTemplate template);
    Task<KnowledgeStructureTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<KnowledgeStructureTemplate?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    Task<KnowledgeStructureTemplate?> GetByIdWithFullTreeAsync(long id, CancellationToken cancellationToken);
    Task<KnowledgeStructureTemplate?> GetByExternalIdWithFullTreeAsync(Guid externalId, CancellationToken cancellationToken);
    Task<KnowledgeStructureTemplate?> GetByExternalIdReadOnlyAsync(Guid externalId, CancellationToken cancellationToken);
    Task<bool> ExistsByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    IQueryable<KnowledgeStructureTemplate> Query();
    Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken);
}
