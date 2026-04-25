using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Commands.SyncFromTemplate;

/// <summary>
/// Handles partial synchronization of a knowledge structure clone from its source template.
/// </summary>
public class SyncFromTemplateHandler : BaseCommandHandler<SyncFromTemplateCommand, PartialSyncResultDto>
{
    private readonly IKnowledgeStructureRepository _structureRepository;
    private readonly IKnowledgeStructureTemplateRepository _templateRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncFromTemplateHandler"/> class.
    /// </summary>
    /// <param name="structureRepository">The knowledge structure (clone) repository.</param>
    /// <param name="templateRepository">The knowledge structure template repository.</param>
    public SyncFromTemplateHandler(
        IKnowledgeStructureRepository structureRepository,
        IKnowledgeStructureTemplateRepository templateRepository)
    {
        _structureRepository = structureRepository;
        _templateRepository = templateRepository;
    }

    /// <inheritdoc />
    public override async Task<Result<PartialSyncResultDto>> Handle(
        SyncFromTemplateCommand request,
        CancellationToken cancellationToken)
    {
        KS? structure = await _structureRepository.GetByExternalIdWithFullTreeAsync(
            request.StructureExternalId,
            cancellationToken);

        if (structure is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Estructura", "La estructura no fue encontrada."));
        }

        if (structure.SourceTemplateId is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("SyncMode", "La estructura no tiene una plantilla de origen."));
        }

        var template = await _templateRepository.GetByIdWithFullTreeAsync(
            structure.SourceTemplateId.Value,
            cancellationToken);

        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Plantilla", "La plantilla de origen no fue encontrada."));
        }

        Domain.Aggregates.KnowledgeStructure.PartialSyncResult result;
        try
        {
            result = structure.ApplyPartialSync(template);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("SyncMode", ex.Message));
        }

        await _structureRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Success(new PartialSyncResultDto(
            result.ModulesAdded,
            result.TopicsAdded,
            result.SubjectsAdded,
            result.ResourcesAdded));
    }
}
