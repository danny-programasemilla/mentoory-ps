using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Queries.GetKnowledgeStructureTemplate;

/// <summary>
/// Handles <see cref="GetKnowledgeStructureTemplateQuery"/> by loading the full aggregate tree read-only
/// from the repository and mapping it to a detail DTO with ordered children at every level.
/// </summary>
public class GetKnowledgeStructureTemplateHandler
    : BaseCommandHandler<GetKnowledgeStructureTemplateQuery, KnowledgeStructureTemplateDetailDto>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetKnowledgeStructureTemplateHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    public GetKnowledgeStructureTemplateHandler(IKnowledgeStructureTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<KnowledgeStructureTemplateDetailDto>> Handle(
        GetKnowledgeStructureTemplateQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByExternalIdReadOnlyAsync(request.ExternalId, cancellationToken);
        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Plantilla", "La plantilla no fue encontrada."));
        }

        var dto = new KnowledgeStructureTemplateDetailDto(
            template.ExternalId,
            template.Name,
            template.Description,
            template.IsArchived,
            template.Version,
            template.CreatedAtUtc,
            template.Modules
                .OrderBy(m => m.SortOrder)
                .Select(m => new ModuleTemplateDto(
                    m.ExternalId,
                    m.Name,
                    m.Description,
                    m.SortOrder,
                    m.Topics
                        .OrderBy(t => t.SortOrder)
                        .Select(t => new TopicTemplateDto(
                            t.ExternalId,
                            t.Name,
                            t.Description,
                            t.SortOrder,
                            t.HighRangeMin,
                            t.HighRangeMax,
                            t.MediumRangeMin,
                            t.MediumRangeMax,
                            t.LowRangeMin,
                            t.LowRangeMax,
                            t.Subjects
                                .OrderBy(s => s.SortOrder)
                                .Select(s => new SubjectTemplateDto(
                                    s.ExternalId,
                                    s.Name,
                                    s.Description,
                                    s.SortOrder,
                                    s.Resources
                                        .OrderBy(r => r.SortOrder)
                                        .Select(r => new ResourceTemplateDto(
                                            r.ExternalId,
                                            r.Title,
                                            r.Description,
                                            r.Url,
                                            r.ResourceType,
                                            r.SortOrder))
                                        .ToList()))
                                .ToList()))
                        .ToList()))
                .ToList());

        return Success(dto);
    }
}
