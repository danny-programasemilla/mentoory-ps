using Mentoory.Knowledge.Domain.Enums;

namespace Mentoory.Knowledge.Application.Queries.GetKnowledgeStructureTemplate;

/// <summary>
/// Data transfer object representing a knowledge structure template with its full child tree.
/// </summary>
/// <param name="ExternalId">The external GUID identifier.</param>
/// <param name="Name">The template name.</param>
/// <param name="Description">The template description.</param>
/// <param name="IsArchived">Whether the template is archived.</param>
/// <param name="Version">The template version number.</param>
/// <param name="CreatedAtUtc">The template creation timestamp in UTC.</param>
/// <param name="Modules">The ordered collection of module templates belonging to this template.</param>
public sealed record KnowledgeStructureTemplateDetailDto(
    Guid ExternalId,
    string Name,
    string? Description,
    bool IsArchived,
    int Version,
    DateTime CreatedAtUtc,
    IReadOnlyList<ModuleTemplateDto> Modules);

/// <summary>
/// Data transfer object representing a module within a knowledge structure template.
/// </summary>
/// <param name="ExternalId">The external GUID identifier.</param>
/// <param name="Name">The module name.</param>
/// <param name="Description">The module description.</param>
/// <param name="SortOrder">The sort order of the module within its template.</param>
/// <param name="Topics">The ordered collection of topics belonging to this module.</param>
public sealed record ModuleTemplateDto(
    Guid ExternalId,
    string Name,
    string? Description,
    int SortOrder,
    IReadOnlyList<TopicTemplateDto> Topics);

/// <summary>
/// Data transfer object representing a topic within a module, including its priority ranges.
/// </summary>
/// <param name="ExternalId">The external GUID identifier.</param>
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
public sealed record TopicTemplateDto(
    Guid ExternalId,
    string Name,
    string? Description,
    int SortOrder,
    decimal? HighRangeMin,
    decimal? HighRangeMax,
    decimal? MediumRangeMin,
    decimal? MediumRangeMax,
    decimal? LowRangeMin,
    decimal? LowRangeMax,
    IReadOnlyList<SubjectTemplateDto> Subjects);

/// <summary>
/// Data transfer object representing a subject within a topic.
/// </summary>
/// <param name="ExternalId">The external GUID identifier.</param>
/// <param name="Name">The subject name.</param>
/// <param name="Description">The subject description.</param>
/// <param name="SortOrder">The sort order of the subject within its topic.</param>
/// <param name="Resources">The ordered collection of resources attached to this subject.</param>
public sealed record SubjectTemplateDto(
    Guid ExternalId,
    string Name,
    string? Description,
    int SortOrder,
    IReadOnlyList<ResourceTemplateDto> Resources);

/// <summary>
/// Data transfer object representing a learning resource attached to a subject.
/// </summary>
/// <param name="ExternalId">The external GUID identifier.</param>
/// <param name="Title">The resource title.</param>
/// <param name="Description">The resource description.</param>
/// <param name="Url">The resource URL.</param>
/// <param name="ResourceType">The type of the resource (video, link, file).</param>
/// <param name="SortOrder">The sort order of the resource within its subject.</param>
public sealed record ResourceTemplateDto(
    Guid ExternalId,
    string Title,
    string? Description,
    string Url,
    ResourceType ResourceType,
    int SortOrder);
