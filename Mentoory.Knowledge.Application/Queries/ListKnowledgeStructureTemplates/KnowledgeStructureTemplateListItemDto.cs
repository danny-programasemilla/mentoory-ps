namespace Mentoory.Knowledge.Application.Queries.ListKnowledgeStructureTemplates;

/// <summary>
/// Data transfer object representing a knowledge structure template in the list view,
/// including aggregate counts of its child modules, topics, subjects, and resources.
/// </summary>
/// <param name="ExternalId">The external GUID identifier used for routing.</param>
/// <param name="Name">The template name.</param>
/// <param name="Description">The template description.</param>
/// <param name="IsArchived">Whether the template is archived.</param>
/// <param name="Version">The template version number, incremented on any structural change.</param>
/// <param name="CreatedAtUtc">The template creation timestamp in UTC.</param>
/// <param name="ModuleCount">The total number of modules in the template.</param>
/// <param name="TopicCount">The total number of topics across all modules.</param>
/// <param name="SubjectCount">The total number of subjects across all topics.</param>
/// <param name="ResourceCount">The total number of resources across all subjects.</param>
public sealed record KnowledgeStructureTemplateListItemDto(
    Guid ExternalId,
    string Name,
    string? Description,
    bool IsArchived,
    int Version,
    DateTime CreatedAtUtc,
    int ModuleCount,
    int TopicCount,
    int SubjectCount,
    int ResourceCount);
