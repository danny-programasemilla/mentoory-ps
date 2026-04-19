using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Repositories;

public class KnowledgeStructureRepository : AbstractRepository<KS>, IKnowledgeStructureRepository
{
    private readonly KnowledgeDbContext _dbContext;

    public KnowledgeStructureRepository(KnowledgeDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new KS Add(KS structure)
    {
        return _dbContext.KnowledgeStructures.Add(structure).Entity;
    }

    public new void Update(KS structure)
    {
        _dbContext.Entry(structure).State = EntityState.Modified;
    }

    public void Remove(KS structure)
    {
        _dbContext.KnowledgeStructures.Remove(structure);
    }

    public Task<KS?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructures
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public Task<KS?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructures
            .FirstOrDefaultAsync(s => s.ExternalId == externalId, cancellationToken);
    }

    // NOTE: AsNoTracking is intentionally absent on the write-path full-tree loaders
    // (GetBy*WithFullTreeAsync and GetByProjectAndSourceTemplateIdAsync). EF must
    // track the aggregate so mutations through the root can be persisted. The
    // parallel GetByExternalIdReadOnlyAsync applies AsNoTracking for query
    // projections, and ListByProjectAsync / Query() are read-only.
    public Task<KS?> GetByIdWithFullTreeAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructures
            .AsSplitQuery()
            .Include(s => s.Modules)
                .ThenInclude(m => m.Topics)
                    .ThenInclude(t => t.Subjects)
                        .ThenInclude(sub => sub.Resources)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public Task<KS?> GetByExternalIdWithFullTreeAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructures
            .AsSplitQuery()
            .Include(s => s.Modules)
                .ThenInclude(m => m.Topics)
                    .ThenInclude(t => t.Subjects)
                        .ThenInclude(sub => sub.Resources)
            .FirstOrDefaultAsync(s => s.ExternalId == externalId, cancellationToken);
    }

    public Task<KS?> GetByExternalIdReadOnlyAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructures
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Modules)
                .ThenInclude(m => m.Topics)
                    .ThenInclude(t => t.Subjects)
                        .ThenInclude(sub => sub.Resources)
            .FirstOrDefaultAsync(s => s.ExternalId == externalId, cancellationToken);
    }

    public Task<KS?> GetByProjectAndSourceTemplateIdAsync(long projectId, long sourceTemplateId, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructures
            .AsSplitQuery()
            .Include(s => s.Modules)
                .ThenInclude(m => m.Topics)
                    .ThenInclude(t => t.Subjects)
                        .ThenInclude(sub => sub.Resources)
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.SourceTemplateId == sourceTemplateId, cancellationToken);
    }

    public async Task<IReadOnlyList<KS>> ListByProjectAsync(long projectId, CancellationToken cancellationToken)
    {
        var results = await _dbContext.KnowledgeStructures
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    public Task<int> CountClonesBySourceTemplateIdAsync(long sourceTemplateId, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructures
            .CountAsync(s => s.SourceTemplateId == sourceTemplateId, cancellationToken);
    }

    public IQueryable<KS> Query()
    {
        return _dbContext.KnowledgeStructures.AsNoTracking();
    }

    public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }
}
