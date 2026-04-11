using Mentoory.Tenant.Domain.Aggregates.ProjectInvitation;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Domain.Repositories;

public interface IProjectInvitationRepository : IRepository<ProjectInvitation>
{
    ProjectInvitation Add(ProjectInvitation invitation);
    void Update(ProjectInvitation invitation);
    Task<ProjectInvitation?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    Task<ProjectInvitation?> GetActiveByUserAndProjectAsync(long userId, long projectId, CancellationToken cancellationToken);
    Task<List<ProjectInvitation>> GetByProjectAsync(long projectId, CancellationToken cancellationToken);
    Task<List<ProjectInvitation>> GetByUserAsync(long userId, CancellationToken cancellationToken);
}
