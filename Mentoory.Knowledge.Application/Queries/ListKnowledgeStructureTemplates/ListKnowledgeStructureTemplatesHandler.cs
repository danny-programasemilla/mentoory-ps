using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Queries.ListKnowledgeStructureTemplates;

/// <summary>
/// Handles <see cref="ListKnowledgeStructureTemplatesQuery"/> by projecting knowledge structure templates
/// to a lightweight list DTO with aggregate child counts computed in the database.
/// </summary>
public class ListKnowledgeStructureTemplatesHandler
    : BaseCommandHandler<ListKnowledgeStructureTemplatesQuery, IReadOnlyList<KnowledgeStructureTemplateListItemDto>>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;

    public ListKnowledgeStructureTemplatesHandler(IKnowledgeStructureTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<IReadOnlyList<KnowledgeStructureTemplateListItemDto>>> Handle(
        ListKnowledgeStructureTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _repository.Query();

        if (!request.IncludeArchived)
        {
            query = query.Where(t => !t.IsArchived);
        }

        var projected = query
            .OrderBy(t => t.Name)
            .Select(t => new KnowledgeStructureTemplateListItemDto(
                t.ExternalId,
                t.Name,
                t.Description,
                t.IsArchived,
                t.Version,
                t.CreatedAtUtc,
                t.Modules.Count(),
                t.Modules.SelectMany(m => m.Topics).Count(),
                t.Modules.SelectMany(m => m.Topics).SelectMany(tp => tp.Subjects).Count(),
                t.Modules.SelectMany(m => m.Topics).SelectMany(tp => tp.Subjects).SelectMany(s => s.Resources).Count()));

        var results = await _repository.ToListAsync(projected, cancellationToken);

        return Success(results);
    }
}
