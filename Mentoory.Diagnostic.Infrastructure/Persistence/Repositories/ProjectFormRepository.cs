using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Diagnostic.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing ProjectForm aggregate roots.
/// </summary>
public class ProjectFormRepository : AbstractRepository<ProjectForm>, IProjectFormRepository
{
    private readonly DiagnosticDbContext _dbContext;

    public ProjectFormRepository(DiagnosticDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public new ProjectForm Add(ProjectForm projectForm)
    {
        return _dbContext.ProjectForms.Add(projectForm).Entity;
    }

    /// <inheritdoc />
    public new void Update(ProjectForm projectForm)
    {
        _dbContext.Entry(projectForm).State = EntityState.Modified;
    }

    /// <inheritdoc />
    public Task<ProjectForm?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectForms
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProjectForm?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectForms
            .FirstOrDefaultAsync(f => f.ExternalId == externalId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProjectForm?> GetByExternalIdAsync(Guid externalId, long projectId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectForms
            .FirstOrDefaultAsync(f => f.ExternalId == externalId && f.ProjectId == projectId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProjectForm?> GetByIdWithQuestionsAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectForms
            .Include(f => f.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .Include(f => f.Questions)
                .ThenInclude(q => q.FollowUpQuestions)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProjectForm?> GetByExternalIdWithQuestionsAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectForms
            .Include(f => f.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .Include(f => f.Questions)
                .ThenInclude(q => q.FollowUpQuestions)
            .FirstOrDefaultAsync(f => f.ExternalId == externalId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProjectForm?> GetByExternalIdWithQuestionsAsync(Guid externalId, long projectId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectForms
            .Include(f => f.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .Include(f => f.Questions)
                .ThenInclude(q => q.FollowUpQuestions)
            .FirstOrDefaultAsync(f => f.ExternalId == externalId && f.ProjectId == projectId, cancellationToken);
    }

    /// <inheritdoc />
    public IQueryable<ProjectForm> Query()
    {
        return _dbContext.ProjectForms.AsNoTracking();
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return _dbContext.ProjectForms.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(IQueryable<ProjectForm> query, CancellationToken cancellationToken)
    {
        return query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }
}
