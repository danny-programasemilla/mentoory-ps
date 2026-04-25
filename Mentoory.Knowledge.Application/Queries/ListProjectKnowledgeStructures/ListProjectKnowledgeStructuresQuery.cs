using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Queries.ListProjectKnowledgeStructures;

/// <summary>
/// Query to retrieve the list of knowledge structures (project clones) for a given project,
/// including aggregate child counts and template version drift indicators.
/// </summary>
/// <param name="ProjectId">The internal project identifier whose knowledge structures are listed.</param>
public sealed record ListProjectKnowledgeStructuresQuery(
    long ProjectId) : IBaseRequest<IReadOnlyList<ProjectKnowledgeStructureListItemDto>>;
