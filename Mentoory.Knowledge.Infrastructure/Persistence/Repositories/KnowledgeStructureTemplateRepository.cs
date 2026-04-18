using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Repositories;

public class KnowledgeStructureTemplateRepository : AbstractRepository<KnowledgeStructureTemplate>, IKnowledgeStructureTemplateRepository
{
    private readonly KnowledgeDbContext _dbContext;

    public KnowledgeStructureTemplateRepository(KnowledgeDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new KnowledgeStructureTemplate Add(KnowledgeStructureTemplate template)
    {
        return _dbContext.KnowledgeStructureTemplates.Add(template).Entity;
    }

    public new void Update(KnowledgeStructureTemplate template)
    {
        _dbContext.Entry(template).State = EntityState.Modified;
    }

    public void Remove(KnowledgeStructureTemplate template)
    {
        _dbContext.KnowledgeStructureTemplates.Remove(template);
    }

    public Task<KnowledgeStructureTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructureTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task<KnowledgeStructureTemplate?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructureTemplates
            .FirstOrDefaultAsync(t => t.ExternalId == externalId, cancellationToken);
    }

    public Task<KnowledgeStructureTemplate?> GetByIdWithFullTreeAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructureTemplates
            .AsSplitQuery()
            .Include(t => t.Modules)
                .ThenInclude(m => m.Topics)
                    .ThenInclude(t => t.Subjects)
                        .ThenInclude(s => s.Resources)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task<KnowledgeStructureTemplate?> GetByExternalIdWithFullTreeAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructureTemplates
            .AsSplitQuery()
            .Include(t => t.Modules)
                .ThenInclude(m => m.Topics)
                    .ThenInclude(t => t.Subjects)
                        .ThenInclude(s => s.Resources)
            .FirstOrDefaultAsync(t => t.ExternalId == externalId, cancellationToken);
    }

    public Task<KnowledgeStructureTemplate?> GetByExternalIdReadOnlyAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructureTemplates
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Modules)
                .ThenInclude(m => m.Topics)
                    .ThenInclude(t => t.Subjects)
                        .ThenInclude(s => s.Resources)
            .FirstOrDefaultAsync(t => t.ExternalId == externalId, cancellationToken);
    }

    public Task<bool> ExistsByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.KnowledgeStructureTemplates
            .AnyAsync(t => t.ExternalId == externalId, cancellationToken);
    }

    public IQueryable<KnowledgeStructureTemplate> Query()
    {
        return _dbContext.KnowledgeStructureTemplates.AsNoTracking();
    }

    public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }
}
