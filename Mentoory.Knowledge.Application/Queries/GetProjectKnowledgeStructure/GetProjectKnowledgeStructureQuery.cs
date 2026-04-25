using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Queries.GetProjectKnowledgeStructure;

/// <summary>
/// Query to retrieve a single project knowledge structure (template clone), including its full
/// module/topic/subject/resource tree.
/// </summary>
/// <param name="ExternalId">The external GUID identifier of the structure to retrieve.</param>
public sealed record GetProjectKnowledgeStructureQuery(
    Guid ExternalId) : IBaseRequest<ProjectKnowledgeStructureDetailDto>;
