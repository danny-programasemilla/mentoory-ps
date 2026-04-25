using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Application.Abstractions;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.CreateProject;

/// <summary>
/// Handler for creating a new project within an incubator. Each project is bound 1:1 to
/// a <c>KnowledgeStructure</c> materialized from a caller-selected template via the
/// cross-module <see cref="IKnowledgeStructureProvisioner"/>.
/// </summary>
public partial class CreateProjectHandler(
    ILogger<CreateProjectHandler> logger,
    IIncubatorRepository incubatorRepository,
    IProjectRepository projectRepository,
    IKnowledgeStructureTemplateRepository knowledgeStructureTemplateRepository,
    IKnowledgeStructureProvisioner knowledgeStructureProvisioner,
    ITimeProvider timeProvider)
    : BaseCommandHandler<CreateProjectCommand, Guid>
{
    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var incubator = await incubatorRepository.GetByExternalIdAsync(request.IncubatorExternalId, cancellationToken);

        if (incubator is null)
        {
            LogIncubatorNotFound(request.IncubatorExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.IncubatorExternalId), "Incubator not found"));
        }

        var templateExists = await knowledgeStructureTemplateRepository.ExistsByExternalIdAsync(
            request.KnowledgeStructureTemplateExternalId, cancellationToken);
        if (!templateExists)
        {
            LogTemplateNotFound(request.KnowledgeStructureTemplateExternalId);
            return Failure(ResultErrorCodes.GenericError,
                ("KnowledgeStructureTemplate", "La plantilla de conocimiento no fue encontrada."));
        }

        var project = Project.Create(
            incubator.Id,
            request.Name,
            request.Description,
            request.KnowledgeStructureTemplateExternalId,
            timeProvider.UtcNow,
            request.IsPublic,
            request.EnrollmentVariant);
        projectRepository.Add(project);

        await projectRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        var ksResult = await knowledgeStructureProvisioner.CloneForProjectAsync(
            request.KnowledgeStructureTemplateExternalId,
            project.Id,
            incubator.Id,
            cancellationToken);

        if (ksResult.IsFailure)
        {
            LogKnowledgeProvisioningFailed(project.ExternalId, request.KnowledgeStructureTemplateExternalId);
            return Failure(ResultErrorCodes.GenericError,
                ("KnowledgeStructure", "Proyecto creado pero falló la creación de la estructura de conocimiento. Contacte a un administrador."));
        }

        LogProjectCreated(project.ExternalId, request.IncubatorExternalId);

        return Success(project.ExternalId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Incubator not found with ExternalId {ExternalId}")]
    partial void LogIncubatorNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "KnowledgeStructureTemplate not found with ExternalId {ExternalId}")]
    partial void LogTemplateNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Project {ProjectExternalId} saved but KS provisioning failed for template {TemplateExternalId}; project persists without a knowledge structure")]
    partial void LogKnowledgeProvisioningFailed(Guid projectExternalId, Guid templateExternalId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Project created with ExternalId {ExternalId} in incubator {IncubatorExternalId}")]
    partial void LogProjectCreated(Guid externalId, Guid incubatorExternalId);
}
