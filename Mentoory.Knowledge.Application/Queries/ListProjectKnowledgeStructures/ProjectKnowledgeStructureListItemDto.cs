using Mentoory.Knowledge.Domain.Enums;

namespace Mentoory.Knowledge.Application.Queries.ListProjectKnowledgeStructures;

/// <summary>
/// Data transfer object representing a project knowledge structure (template clone) in the list view,
/// including source-template version drift and aggregate child counts.
/// </summary>
/// <param name="ExternalId">The external GUID identifier used for routing.</param>
/// <param name="Name">The structure name.</param>
/// <param name="Description">The structure description.</param>
/// <param name="ProjectId">The owning project identifier.</param>
/// <param name="IncubatorId">The owning incubator identifier.</param>
/// <param name="SourceTemplateExternalId">The external identifier of the source template, or <c>null</c> when the source template cannot be resolved.</param>
/// <param name="SourceTemplateVersion">The template version captured at clone time, or <c>null</c> when the structure was not cloned from a template.</param>
/// <param name="CurrentTemplateVersion">The current version of the source template, or <c>null</c> when no source template is linked.</param>
/// <param name="HasTemplateVersionDrift">Whether the source template has advanced past the version captured at clone time.</param>
/// <param name="SyncMode">The configured synchronization mode with the source template.</param>
/// <param name="CreatedAtUtc">The structure creation timestamp in UTC.</param>
/// <param name="ModuleCount">The total number of modules in the structure.</param>
/// <param name="TopicCount">The total number of topics across all modules.</param>
public sealed record ProjectKnowledgeStructureListItemDto(
    Guid ExternalId,
    string Name,
    string? Description,
    long ProjectId,
    long IncubatorId,
    Guid? SourceTemplateExternalId,
    int? SourceTemplateVersion,
    int? CurrentTemplateVersion,
    bool HasTemplateVersionDrift,
    SyncMode SyncMode,
    DateTime CreatedAtUtc,
    int ModuleCount,
    int TopicCount);
