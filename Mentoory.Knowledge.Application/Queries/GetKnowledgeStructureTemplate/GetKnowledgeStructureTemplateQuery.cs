using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Queries.GetKnowledgeStructureTemplate;

/// <summary>
/// Query to retrieve a single knowledge structure template, including its full module/topic/subject/resource tree.
/// </summary>
/// <param name="ExternalId">The external GUID identifier of the template to retrieve.</param>
public sealed record GetKnowledgeStructureTemplateQuery(
    Guid ExternalId) : IBaseRequest<KnowledgeStructureTemplateDetailDto>;
