using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Knowledge.Domain.Repositories;

public interface IKnowledgeStructureRepository : IRepository<KnowledgeStructure>
{
    KnowledgeStructure Add(KnowledgeStructure structure);
    void Update(KnowledgeStructure structure);
    void Remove(KnowledgeStructure structure);
    Task<KnowledgeStructure?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<KnowledgeStructure?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    Task<KnowledgeStructure?> GetByIdWithFullTreeAsync(long id, CancellationToken cancellationToken);
    Task<KnowledgeStructure?> GetByExternalIdWithFullTreeAsync(Guid externalId, CancellationToken cancellationToken);
    Task<KnowledgeStructure?> GetByExternalIdReadOnlyAsync(Guid externalId, CancellationToken cancellationToken);
    Task<KnowledgeStructure?> GetByProjectAndSourceTemplateIdAsync(long projectId, long sourceTemplateId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeStructure>> ListByProjectAsync(long projectId, CancellationToken cancellationToken);
    Task<int> CountClonesBySourceTemplateIdAsync(long sourceTemplateId, CancellationToken cancellationToken);
    IQueryable<KnowledgeStructure> Query();
    Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken);
}
