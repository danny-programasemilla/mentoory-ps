using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Queries.ListKnowledgeStructureTemplates;

/// <summary>
/// Query to retrieve the list of knowledge structure templates with aggregate child counts.
/// </summary>
/// <param name="IncludeArchived">Whether to include archived templates in the result. When <c>false</c>, only active templates are returned.</param>
public sealed record ListKnowledgeStructureTemplatesQuery(
    bool IncludeArchived) : IBaseRequest<IReadOnlyList<KnowledgeStructureTemplateListItemDto>>;
