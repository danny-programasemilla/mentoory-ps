using Mentoory.Diagnostic.Domain.Aggregates.StageFormAssignment;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Repositories;

public interface IStageFormAssignmentRepository : IRepository<StageFormAssignment>
{
    StageFormAssignment Add(StageFormAssignment assignment);
    void Update(StageFormAssignment assignment);
    Task<StageFormAssignment?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<StageFormAssignment?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    Task<List<StageFormAssignment>> GetByProjectStageIdAsync(long projectStageId, CancellationToken cancellationToken);
    IQueryable<StageFormAssignment> Query();
    Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken);
}
