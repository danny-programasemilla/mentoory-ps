using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Diagnostic.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing FormTemplate aggregate roots.
/// </summary>
public class FormTemplateRepository : AbstractRepository<FormTemplate>, IFormTemplateRepository
{
    private readonly DiagnosticDbContext _dbContext;

    public FormTemplateRepository(DiagnosticDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public new FormTemplate Add(FormTemplate formTemplate)
    {
        return _dbContext.FormTemplates.Add(formTemplate).Entity;
    }

    /// <inheritdoc />
    public new void Update(FormTemplate formTemplate)
    {
        _dbContext.Entry(formTemplate).State = EntityState.Modified;
    }

    /// <inheritdoc />
    public Task<FormTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.FormTemplates
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FormTemplate?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.FormTemplates
            .FirstOrDefaultAsync(f => f.ExternalId == externalId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FormTemplate?> GetByIdWithQuestionsAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.FormTemplates
            .Include(f => f.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FormTemplate?> GetByExternalIdWithQuestionsAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.FormTemplates
            .Include(f => f.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(f => f.ExternalId == externalId, cancellationToken);
    }

    /// <inheritdoc />
    public IQueryable<FormTemplate> Query()
    {
        return _dbContext.FormTemplates.AsNoTracking();
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return _dbContext.FormTemplates.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(IQueryable<FormTemplate> query, CancellationToken cancellationToken)
    {
        return query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }
}
