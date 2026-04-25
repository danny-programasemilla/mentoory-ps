using Mentoory.Knowledge.Domain.Enums;

namespace Mentoory.Knowledge.Application.Queries.GetProjectKnowledgeStructure;

/// <summary>
/// Data transfer object representing a project knowledge structure with its full child tree.
/// </summary>
/// <param name="ExternalId">The external GUID identifier.</param>
/// <param name="Name">The structure name.</param>
/// <param name="Description">The structure description.</param>
/// <param name="ProjectId">The owning project identifier.</param>
/// <param name="IncubatorId">The owning incubator identifier.</param>
/// <param name="SourceTemplateExternalId">The external identifier of the source template, or <c>null</c> when not resolved in v1.</param>
/// <param name="SourceTemplateVersion">The template version captured at clone time, or <c>null</c> when the structure was not cloned from a template.</param>
/// <param name="SyncMode">The configured synchronization mode with the source template.</param>
/// <param name="CreatedAtUtc">The structure creation timestamp in UTC.</param>
/// <param name="Modules">The ordered collection of modules belonging to this structure.</param>
public sealed record ProjectKnowledgeStructureDetailDto(
    Guid ExternalId,
    string Name,
    string? Description,
    long ProjectId,
    long IncubatorId,
    Guid? SourceTemplateExternalId,
    int? SourceTemplateVersion,
    SyncMode SyncMode,
    DateTime CreatedAtUtc,
    IReadOnlyList<ModuleDto> Modules);

/// <summary>
/// Data transfer object representing a module within a project knowledge structure.
/// </summary>
/// <param name="ExternalId">The external GUID identifier.</param>
/// <param name="SourceTemplateModuleExternalId">The external identifier of the source template module, or <c>null</c> when added after cloning.</param>
/// <param name="Name">The module name.</param>
/// <param name="Description">The module description.</param>
/// <param name="SortOrder">The sort order of the module within its structure.</param>
/// <param name="Topics">The ordered collection of topics belonging to this module.</param>
public sealed record ModuleDto(
    Guid ExternalId,
    Guid? SourceTemplateModuleExternalId,
    string Name,
    string? Description,
    int SortOrder,
    IReadOnlyList<TopicDto> Topics);

/// <summary>
/// Data transfer object representing a topic within a module, including its priority ranges.
/// </summary>
/// <param name="ExternalId">The external GUID identifier.</param>
/// <param name="SourceTemplateTopicExternalId">The external identifier of the source template topic, or <c>null</c> when added after cloning.</param>
/// <param name="Name">The topic name.</param>
/// <param name="Description">The topic description.</param>
/// <param name="SortOrder">The sort order of the topic within its module.</param>
/// <param name="HighRangeMin">The minimum value of the high priority range, if configured.</param>
/// <param name="HighRangeMax">The maximum value of the high priority range, if configured.</param>
/// <param name="MediumRangeMin">The minimum value of the medium priority range, if configured.</param>
/// <param name="MediumRangeMax">The maximum value of the medium priority range, if configured.</param>
/// <param name="LowRangeMin">The minimum value of the low priority range, if configured.</param>
/// <param name="LowRangeMax">The maximum value of the low priority range, if configured.</param>
/// <param name="Subjects">The ordered collection of subjects belonging to this topic.</param>
public sealed record TopicDto(
    Guid ExternalId,
    Guid? SourceTemplateTopicExternalId,
    string Name,
    string? Description,
    int SortOrder,
    decimal? HighRangeMin,
    decimal? HighRangeMax,
    decimal? MediumRangeMin,
    decimal? MediumRangeMax,
    decimal? LowRangeMin,
    decimal? LowRangeMax,
    IReadOnlyList<SubjectDto> Subjects);

/// <summary>
/// Data transfer object representing a subject within a topic.
/// </summary>
/// <param name="ExternalId">The external GUID identifier.</param>
/// <param name="SourceTemplateSubjectExternalId">The external identifier of the source template subject, or <c>null</c> when added after cloning.</param>
/// <param name="Name">The subject name.</param>
/// <param name="Description">The subject description.</param>
/// <param name="SortOrder">The sort order of the subject within its topic.</param>
/// <param name="Resources">The ordered collection of resources attached to this subject.</param>
public sealed record SubjectDto(
    Guid ExternalId,
    Guid? SourceTemplateSubjectExternalId,
    string Name,
    string? Description,
    int SortOrder,
    IReadOnlyList<ResourceDto> Resources);

/// <summary>
/// Data transfer object representing a learning resource attached to a subject.
/// </summary>
/// <param name="ExternalId">The external GUID identifier.</param>
/// <param name="SourceTemplateResourceExternalId">The external identifier of the source template resource, or <c>null</c> when added after cloning.</param>
/// <param name="Title">The resource title.</param>
/// <param name="Description">The resource description.</param>
/// <param name="Url">The resource URL.</param>
/// <param name="ResourceType">The type of the resource (video, link, file).</param>
/// <param name="SortOrder">The sort order of the resource within its subject.</param>
public sealed record ResourceDto(
    Guid ExternalId,
    Guid? SourceTemplateResourceExternalId,
    string Title,
    string? Description,
    string Url,
    ResourceType ResourceType,
    int SortOrder);
