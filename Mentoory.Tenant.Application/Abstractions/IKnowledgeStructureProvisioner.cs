using Mentoory.Shared.Application;

namespace Mentoory.Tenant.Application.Abstractions;

/// <summary>
/// Cross-module provisioner that materializes a per-project <c>KnowledgeStructure</c>
/// cloned from a selected <c>KnowledgeStructureTemplate</c>. Invoked by Tenant-side
/// project creation; implemented in the Knowledge module.
/// </summary>
public interface IKnowledgeStructureProvisioner
{
    /// <summary>
    /// Clones the given KS template into a new project-scoped KnowledgeStructure and persists it.
    /// </summary>
    /// <returns>
    /// A <see cref="Result{Guid}"/> carrying the new structure's ExternalId on success, or a
    /// localized Spanish failure message (template missing/archived, etc.).
    /// </returns>
    Task<Result<Guid>> CloneForProjectAsync(
        Guid templateExternalId,
        long projectId,
        long incubatorId,
        CancellationToken cancellationToken);
}
