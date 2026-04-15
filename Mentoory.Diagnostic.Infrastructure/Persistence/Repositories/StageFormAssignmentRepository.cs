using Mentoory.Diagnostic.Domain.Aggregates.StageFormAssignment;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Diagnostic.Infrastructure.Persistence.Repositories;

public class StageFormAssignmentRepository : AbstractRepository<StageFormAssignment>, IStageFormAssignmentRepository
{
    private readonly DiagnosticDbContext _dbContext;

    public StageFormAssignmentRepository(DiagnosticDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new StageFormAssignment Add(StageFormAssignment assignment)
    {
        return _dbContext.StageFormAssignments.Add(assignment).Entity;
    }

    public new void Update(StageFormAssignment assignment)
    {
        _dbContext.Entry(assignment).State = EntityState.Modified;
    }

    public Task<StageFormAssignment?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.StageFormAssignments
            .Include(a => a.AssignedQuestions)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<StageFormAssignment?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.StageFormAssignments
            .Include(a => a.AssignedQuestions)
            .FirstOrDefaultAsync(a => a.ExternalId == externalId, cancellationToken);
    }

    public Task<List<StageFormAssignment>> GetByProjectStageIdAsync(long projectStageId, CancellationToken cancellationToken)
    {
        return _dbContext.StageFormAssignments
            .Include(a => a.AssignedQuestions)
            .Where(a => a.ProjectStageId == projectStageId && a.IsActive)
            .ToListAsync(cancellationToken);
    }

    public IQueryable<StageFormAssignment> Query()
    {
        return _dbContext.StageFormAssignments
            .Include(a => a.AssignedQuestions)
            .AsNoTracking();
    }

    public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }
}
