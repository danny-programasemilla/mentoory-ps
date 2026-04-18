using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Commands.CloneKnowledgeStructureTemplate;

/// <summary>
/// Handles the cloning of a knowledge structure template into a new project-scoped structure.
/// </summary>
public partial class CloneKnowledgeStructureTemplateHandler
    : BaseCommandHandler<CloneKnowledgeStructureTemplateCommand, Guid>
{
    private readonly IKnowledgeStructureTemplateRepository _templateRepository;
    private readonly IKnowledgeStructureRepository _structureRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<CloneKnowledgeStructureTemplateHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneKnowledgeStructureTemplateHandler"/> class.
    /// </summary>
    /// <param name="templateRepository">The knowledge structure template repository.</param>
    /// <param name="structureRepository">The knowledge structure repository.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
    public CloneKnowledgeStructureTemplateHandler(
        IKnowledgeStructureTemplateRepository templateRepository,
        IKnowledgeStructureRepository structureRepository,
        ITimeProvider timeProvider,
        ILogger<CloneKnowledgeStructureTemplateHandler> logger)
    {
        _templateRepository = templateRepository;
        _structureRepository = structureRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(
        CloneKnowledgeStructureTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByExternalIdWithFullTreeAsync(
            request.SourceTemplateExternalId,
            cancellationToken);

        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Plantilla", "La plantilla de origen no fue encontrada."));
        }

        if (template.IsArchived)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Plantilla", "No se puede clonar una plantilla archivada."));
        }

        try
        {
            var clone = KS.CloneFromTemplate(
                template,
                request.ProjectId,
                request.IncubatorId,
                _timeProvider.UtcNow);

            _structureRepository.Add(clone);
            await _structureRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            LogStructureCloned(clone.ExternalId, request.SourceTemplateExternalId, request.ProjectId);

            return Success(clone.ExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }
    }

    /// <summary>
    /// Logs when a knowledge structure clone is successfully created from a template.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Knowledge structure {ExternalId} cloned from template {TemplateExternalId} for project {ProjectId}")]
    partial void LogStructureCloned(Guid externalId, Guid templateExternalId, long projectId);
}
