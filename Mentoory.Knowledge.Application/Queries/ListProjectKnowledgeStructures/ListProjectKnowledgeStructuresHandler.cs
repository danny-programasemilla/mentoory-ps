using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Queries.ListProjectKnowledgeStructures;

/// <summary>
/// Handles <see cref="ListProjectKnowledgeStructuresQuery"/> by projecting the project's knowledge structures
/// to a lightweight list DTO, joining against templates to compute version drift in the database.
/// </summary>
public class ListProjectKnowledgeStructuresHandler
    : BaseCommandHandler<ListProjectKnowledgeStructuresQuery, IReadOnlyList<ProjectKnowledgeStructureListItemDto>>
{
    private readonly IKnowledgeStructureRepository _repository;
    private readonly IKnowledgeStructureTemplateRepository _templateRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListProjectKnowledgeStructuresHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure repository.</param>
    /// <param name="templateRepository">The knowledge structure template repository used to compute version drift.</param>
    public ListProjectKnowledgeStructuresHandler(
        IKnowledgeStructureRepository repository,
        IKnowledgeStructureTemplateRepository templateRepository)
    {
        _repository = repository;
        _templateRepository = templateRepository;
    }

    /// <inheritdoc />
    public override async Task<Result<IReadOnlyList<ProjectKnowledgeStructureListItemDto>>> Handle(
        ListProjectKnowledgeStructuresQuery request,
        CancellationToken cancellationToken)
    {
        var structuresQuery = _repository.Query().Where(s => s.ProjectId == request.ProjectId);
        var templatesQuery = _templateRepository.Query();

        var projected = structuresQuery
            .OrderBy(s => s.Name)
            .Select(s => new ProjectKnowledgeStructureListItemDto(
                s.ExternalId,
                s.Name,
                s.Description,
                s.ProjectId,
                s.IncubatorId,
                templatesQuery
                    .Where(t => t.Id == s.SourceTemplateId)
                    .Select(t => (Guid?)t.ExternalId)
                    .FirstOrDefault(),
                s.SourceTemplateVersion,
                templatesQuery
                    .Where(t => t.Id == s.SourceTemplateId)
                    .Select(t => (int?)t.Version)
                    .FirstOrDefault(),
                s.SourceTemplateId.HasValue
                    && s.SourceTemplateVersion.HasValue
                    && templatesQuery.Any(t => t.Id == s.SourceTemplateId && t.Version != s.SourceTemplateVersion.Value),
                s.SyncMode,
                s.CreatedAtUtc,
                s.Modules.Count(),
                s.Modules.SelectMany(m => m.Topics).Count()));

        var results = await _repository.ToListAsync(projected, cancellationToken);

        return Success(results);
    }
}
