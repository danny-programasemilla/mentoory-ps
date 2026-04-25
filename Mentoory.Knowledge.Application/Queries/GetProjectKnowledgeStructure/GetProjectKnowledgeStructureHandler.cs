using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Queries.GetProjectKnowledgeStructure;

/// <summary>
/// Handles <see cref="GetProjectKnowledgeStructureQuery"/> by loading the full aggregate tree read-only
/// from the repository and mapping it to a detail DTO with ordered children at every level.
/// </summary>
public class GetProjectKnowledgeStructureHandler
    : BaseCommandHandler<GetProjectKnowledgeStructureQuery, ProjectKnowledgeStructureDetailDto>
{
    private readonly IKnowledgeStructureRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProjectKnowledgeStructureHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure repository.</param>
    public GetProjectKnowledgeStructureHandler(IKnowledgeStructureRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<ProjectKnowledgeStructureDetailDto>> Handle(
        GetProjectKnowledgeStructureQuery request,
        CancellationToken cancellationToken)
    {
        KS? structure = await _repository.GetByExternalIdReadOnlyAsync(request.ExternalId, cancellationToken);
        if (structure is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Estructura", "La estructura no fue encontrada."));
        }

        var dto = new ProjectKnowledgeStructureDetailDto(
            structure.ExternalId,
            structure.Name,
            structure.Description,
            structure.ProjectId,
            structure.IncubatorId,
            null, // SourceTemplateExternalId — not directly available from the clone; leave null for v1 (UI can fetch separately if needed)
            structure.SourceTemplateVersion,
            structure.SyncMode,
            structure.CreatedAtUtc,
            structure.Modules
                .OrderBy(m => m.SortOrder)
                .Select(m => new ModuleDto(
                    m.ExternalId,
                    m.SourceTemplateModuleExternalId,
                    m.Name,
                    m.Description,
                    m.SortOrder,
                    m.Topics
                        .OrderBy(t => t.SortOrder)
                        .Select(t => new TopicDto(
                            t.ExternalId,
                            t.SourceTemplateTopicExternalId,
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
                                .Select(s => new SubjectDto(
                                    s.ExternalId,
                                    s.SourceTemplateSubjectExternalId,
                                    s.Name,
                                    s.Description,
                                    s.SortOrder,
                                    s.Resources
                                        .OrderBy(r => r.SortOrder)
                                        .Select(r => new ResourceDto(
                                            r.ExternalId,
                                            r.SourceTemplateResourceExternalId,
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
