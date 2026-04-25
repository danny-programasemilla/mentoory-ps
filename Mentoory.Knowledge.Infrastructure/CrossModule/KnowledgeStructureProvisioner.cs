using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Application.Abstractions;
using Microsoft.Extensions.Logging;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Infrastructure.CrossModule;

/// <summary>
/// Knowledge-side implementation of <see cref="IKnowledgeStructureProvisioner"/>. Loads
/// the KS template full tree, calls the <c>CloneFromTemplate</c> factory, persists the new
/// project-scoped structure. Invoked from Tenant-side <c>CreateProjectHandler</c>.
/// </summary>
public partial class KnowledgeStructureProvisioner : IKnowledgeStructureProvisioner
{
    private readonly IKnowledgeStructureTemplateRepository _templateRepository;
    private readonly IKnowledgeStructureRepository _structureRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<KnowledgeStructureProvisioner> _logger;

    public KnowledgeStructureProvisioner(
        IKnowledgeStructureTemplateRepository templateRepository,
        IKnowledgeStructureRepository structureRepository,
        ITimeProvider timeProvider,
        ILogger<KnowledgeStructureProvisioner> logger)
    {
        _templateRepository = templateRepository;
        _structureRepository = structureRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<Guid>> CloneForProjectAsync(
        Guid templateExternalId,
        long projectId,
        long incubatorId,
        CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByExternalIdWithFullTreeAsync(templateExternalId, cancellationToken);
        if (template is null)
        {
            return Result<Guid>.Failure(ResultErrorCodes.GenericError,
                ("Plantilla", "La plantilla de conocimiento no fue encontrada."));
        }

        if (template.IsArchived)
        {
            return Result<Guid>.Failure(ResultErrorCodes.GenericError,
                ("Plantilla", "No se puede crear un proyecto con una plantilla de conocimiento archivada."));
        }

        try
        {
            var structure = KS.CloneFromTemplate(template, projectId, incubatorId, _timeProvider.UtcNow);
            _structureRepository.Add(structure);
            await _structureRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            LogStructureProvisioned(structure.ExternalId, templateExternalId, projectId);
            return Result.Success(structure.ExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Knowledge structure {ExternalId} provisioned from template {TemplateExternalId} for project {ProjectId}")]
    partial void LogStructureProvisioned(Guid externalId, Guid templateExternalId, long projectId);
}
