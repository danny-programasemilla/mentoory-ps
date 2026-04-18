using Mentoory.Knowledge.Application.Commands.AddModule;
using Mentoory.Knowledge.Application.Commands.AddModuleTemplate;
using Mentoory.Knowledge.Application.Commands.AddResource;
using Mentoory.Knowledge.Application.Commands.AddResourceTemplate;
using Mentoory.Knowledge.Application.Commands.AddSubject;
using Mentoory.Knowledge.Application.Commands.AddSubjectTemplate;
using Mentoory.Knowledge.Application.Commands.AddTopic;
using Mentoory.Knowledge.Application.Commands.AddTopicTemplate;
using Mentoory.Knowledge.Application.Commands.ArchiveKnowledgeStructureTemplate;
using Mentoory.Knowledge.Application.Commands.CloneKnowledgeStructureTemplate;
using Mentoory.Knowledge.Application.Commands.CreateKnowledgeStructureTemplate;
using Mentoory.Knowledge.Application.Commands.DeleteKnowledgeStructureTemplate;
using Mentoory.Knowledge.Application.Commands.DeleteModule;
using Mentoory.Knowledge.Application.Commands.DeleteModuleTemplate;
using Mentoory.Knowledge.Application.Commands.DeleteResource;
using Mentoory.Knowledge.Application.Commands.DeleteResourceTemplate;
using Mentoory.Knowledge.Application.Commands.DeleteSubject;
using Mentoory.Knowledge.Application.Commands.DeleteSubjectTemplate;
using Mentoory.Knowledge.Application.Commands.DeleteTopic;
using Mentoory.Knowledge.Application.Commands.DeleteTopicTemplate;
using Mentoory.Knowledge.Application.Commands.ReorderModules;
using Mentoory.Knowledge.Application.Commands.ReorderModuleTemplates;
using Mentoory.Knowledge.Application.Commands.ReorderResources;
using Mentoory.Knowledge.Application.Commands.ReorderResourceTemplates;
using Mentoory.Knowledge.Application.Commands.ReorderSubjects;
using Mentoory.Knowledge.Application.Commands.ReorderSubjectTemplates;
using Mentoory.Knowledge.Application.Commands.ReorderTopics;
using Mentoory.Knowledge.Application.Commands.ReorderTopicTemplates;
using Mentoory.Knowledge.Application.Commands.SetSyncMode;
using Mentoory.Knowledge.Application.Commands.SyncFromTemplate;
using Mentoory.Knowledge.Application.Commands.UnarchiveKnowledgeStructureTemplate;
using Mentoory.Knowledge.Application.Commands.UpdateKnowledgeStructure;
using Mentoory.Knowledge.Application.Commands.UpdateKnowledgeStructureTemplate;
using Mentoory.Knowledge.Application.Commands.UpdateModule;
using Mentoory.Knowledge.Application.Commands.UpdateModuleTemplate;
using Mentoory.Knowledge.Application.Commands.UpdateResource;
using Mentoory.Knowledge.Application.Commands.UpdateResourceTemplate;
using Mentoory.Knowledge.Application.Commands.UpdateSubject;
using Mentoory.Knowledge.Application.Commands.UpdateSubjectTemplate;
using Mentoory.Knowledge.Application.Commands.UpdateTopic;
using Mentoory.Knowledge.Application.Commands.UpdateTopicPriorityRanges;
using Mentoory.Knowledge.Application.Commands.UpdateTopicTemplate;
using Mentoory.Knowledge.Application.Commands.UpdateTopicTemplatePriorityRanges;
using Mentoory.Knowledge.Application.Queries.GetKnowledgeStructureTemplate;
using Mentoory.Knowledge.Application.Queries.GetProjectKnowledgeStructure;
using Mentoory.Knowledge.Application.Queries.ListKnowledgeStructureTemplates;
using Mentoory.Knowledge.Application.Queries.ListProjectKnowledgeStructures;
using Mentoory.Shared.Application;
using Mentoory.Web.Areas.Coordination.Models.Knowledge;
using Mentoory.Web.Infrastructure;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Coordination.Controllers;

[Area("Coordination")]
[Route("[area]/[controller]")]
[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
public class KnowledgeController : Controller
{
    private readonly MediatRExecutor _executor;

    public KnowledgeController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    // -------------------------------------------------------------------------
    // Template list + detail (views)
    // -------------------------------------------------------------------------
    [HttpGet("Templates")]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> Templates(bool includeArchived = false, CancellationToken ct = default)
    {
        var templates = await _executor.SendOrThrowAsync(
            new ListKnowledgeStructureTemplatesQuery(includeArchived), ct);

        ViewBag.IncludeArchived = includeArchived;
        return View(templates);
    }

    [HttpGet("Templates/{externalId:guid}")]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> TemplateDetail(Guid externalId, CancellationToken ct)
    {
        try
        {
            var detail = await _executor.SendOrThrowAsync(
                new GetKnowledgeStructureTemplateQuery(externalId), ct);

            return View(detail);
        }
        catch (InvalidOperationException)
        {
            TempData["WarningMessage"] = "La plantilla solicitada no existe o no está disponible.";
            return RedirectToAction(nameof(Templates));
        }
    }

    [HttpGet("Templates/Create")]
    [Authorize(Roles = "GlobalAdmin")]
    public IActionResult CreateTemplate()
    {
        return View(new CreateTemplateViewModel());
    }

    [HttpPost("Templates/Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> CreateTemplate(CreateTemplateViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new CreateKnowledgeStructureTemplateCommand(model.Name, model.Description), ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Plantilla creada exitosamente.";
            return RedirectToAction(nameof(TemplateDetail), new { externalId = result.Value });
        }

        ModelState.AddModelError(string.Empty, FirstErrorMessage(result) ?? "Error al crear la plantilla.");
        return View(model);
    }

    [HttpPost("Templates/{externalId:guid}/Update")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> UpdateTemplate(Guid externalId, [FromBody] UpdateTemplateViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateKnowledgeStructureTemplateCommand(externalId, model.Name, model.Description), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{externalId:guid}/Archive")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> ArchiveTemplate(Guid externalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new ArchiveKnowledgeStructureTemplateCommand(externalId), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{externalId:guid}/Unarchive")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> UnarchiveTemplate(Guid externalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new UnarchiveKnowledgeStructureTemplateCommand(externalId), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{externalId:guid}/Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> DeleteTemplate(Guid externalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new DeleteKnowledgeStructureTemplateCommand(externalId), ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Plantilla eliminada exitosamente.";
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Templates)) });
        }

        return JsonFailure(result);
    }

    // -------------------------------------------------------------------------
    // Modules
    // -------------------------------------------------------------------------
    [HttpPost("Templates/{templateExternalId:guid}/Modules/Add")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> AddModule(Guid templateExternalId, [FromBody] AddModuleInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new AddModuleTemplateCommand(templateExternalId, input.Name, input.Description, input.SortOrder), ct);

        return result.IsSuccess
            ? JsonSuccess(new { externalId = result.Value })
            : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Modules/{moduleExternalId:guid}/Update")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> UpdateModule(Guid templateExternalId, Guid moduleExternalId, [FromBody] UpdateModuleInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateModuleTemplateCommand(templateExternalId, moduleExternalId, input.Name, input.Description), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Modules/{moduleExternalId:guid}/Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> DeleteModule(Guid templateExternalId, Guid moduleExternalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new DeleteModuleTemplateCommand(templateExternalId, moduleExternalId), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Modules/Reorder")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> ReorderModules(Guid templateExternalId, [FromBody] ReorderInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new ReorderModuleTemplatesCommand(templateExternalId, input.ExternalIds), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    // -------------------------------------------------------------------------
    // Topics
    // -------------------------------------------------------------------------
    [HttpPost("Templates/{templateExternalId:guid}/Topics/Add")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> AddTopic(Guid templateExternalId, [FromBody] AddTopicInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new AddTopicTemplateCommand(templateExternalId, input.ModuleExternalId, input.Name, input.Description, input.SortOrder), ct);

        return result.IsSuccess
            ? JsonSuccess(new { externalId = result.Value })
            : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Topics/{topicExternalId:guid}/Update")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> UpdateTopic(Guid templateExternalId, Guid topicExternalId, [FromBody] UpdateTopicInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateTopicTemplateCommand(templateExternalId, topicExternalId, input.Name, input.Description), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Topics/{topicExternalId:guid}/Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> DeleteTopic(Guid templateExternalId, Guid topicExternalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new DeleteTopicTemplateCommand(templateExternalId, topicExternalId), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Modules/{moduleExternalId:guid}/Topics/Reorder")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> ReorderTopics(Guid templateExternalId, Guid moduleExternalId, [FromBody] ReorderInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new ReorderTopicTemplatesCommand(templateExternalId, moduleExternalId, input.ExternalIds), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Topics/{topicExternalId:guid}/PriorityRanges")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> UpdateTopicPriorityRanges(Guid templateExternalId, Guid topicExternalId, [FromBody] UpdateTopicPriorityRangesInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateTopicTemplatePriorityRangesCommand(
                templateExternalId,
                topicExternalId,
                ToPriorityRange(input.High),
                ToPriorityRange(input.Medium),
                ToPriorityRange(input.Low)), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    // -------------------------------------------------------------------------
    // Subjects
    // -------------------------------------------------------------------------
    [HttpPost("Templates/{templateExternalId:guid}/Subjects/Add")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> AddSubject(Guid templateExternalId, [FromBody] AddSubjectInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new AddSubjectTemplateCommand(templateExternalId, input.TopicExternalId, input.Name, input.Description, input.SortOrder), ct);

        return result.IsSuccess
            ? JsonSuccess(new { externalId = result.Value })
            : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Subjects/{subjectExternalId:guid}/Update")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> UpdateSubject(Guid templateExternalId, Guid subjectExternalId, [FromBody] UpdateSubjectInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateSubjectTemplateCommand(templateExternalId, subjectExternalId, input.Name, input.Description), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Subjects/{subjectExternalId:guid}/Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> DeleteSubject(Guid templateExternalId, Guid subjectExternalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new DeleteSubjectTemplateCommand(templateExternalId, subjectExternalId), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Topics/{topicExternalId:guid}/Subjects/Reorder")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> ReorderSubjects(Guid templateExternalId, Guid topicExternalId, [FromBody] ReorderInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new ReorderSubjectTemplatesCommand(templateExternalId, topicExternalId, input.ExternalIds), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    // -------------------------------------------------------------------------
    // Resources
    // -------------------------------------------------------------------------
    [HttpPost("Templates/{templateExternalId:guid}/Resources/Add")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> AddResource(Guid templateExternalId, [FromBody] AddResourceInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new AddResourceTemplateCommand(
                templateExternalId,
                input.SubjectExternalId,
                input.Title,
                input.Description,
                input.Url,
                input.ResourceType,
                input.SortOrder), ct);

        return result.IsSuccess
            ? JsonSuccess(new { externalId = result.Value })
            : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Resources/{resourceExternalId:guid}/Update")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> UpdateResource(Guid templateExternalId, Guid resourceExternalId, [FromBody] UpdateResourceInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateResourceTemplateCommand(
                templateExternalId,
                resourceExternalId,
                input.Title,
                input.Description,
                input.Url,
                input.ResourceType), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Resources/{resourceExternalId:guid}/Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> DeleteResource(Guid templateExternalId, Guid resourceExternalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new DeleteResourceTemplateCommand(templateExternalId, resourceExternalId), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Templates/{templateExternalId:guid}/Subjects/{subjectExternalId:guid}/Resources/Reorder")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> ReorderResources(Guid templateExternalId, Guid subjectExternalId, [FromBody] ReorderInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new ReorderResourceTemplatesCommand(templateExternalId, subjectExternalId, input.ExternalIds), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    // =========================================================================
    // Project clone actions (US2)
    // =========================================================================

    // -------------------------------------------------------------------------
    // Project clone list + detail (views)
    // -------------------------------------------------------------------------
    [HttpGet("Projects")]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> Projects(CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto antes de ver las estructuras de conocimiento.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var structures = await _executor.SendOrThrowAsync(
            new ListProjectKnowledgeStructuresQuery(projectId.Value), ct);

        return View(structures);
    }

    [HttpGet("Projects/{externalId:guid}")]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> ProjectStructureDetail(Guid externalId, CancellationToken ct)
    {
        try
        {
            var detail = await _executor.SendOrThrowAsync(
                new GetProjectKnowledgeStructureQuery(externalId), ct);

            return View(detail);
        }
        catch (InvalidOperationException)
        {
            TempData["WarningMessage"] = "La estructura solicitada no existe o no está disponible.";
            return RedirectToAction(nameof(Projects));
        }
    }

    [HttpGet("Projects/Clone")]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> CloneFromTemplateGet(CancellationToken ct)
    {
        if (!User.GetActiveProjectId().HasValue)
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto antes de clonar una plantilla.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var templates = await _executor.SendOrThrowAsync(
            new ListKnowledgeStructureTemplatesQuery(IncludeArchived: false), ct);

        ViewBag.Templates = templates;
        return View("CloneFromTemplate", new CloneStructureViewModel());
    }

    [HttpPost("Projects/Clone")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> CloneFromTemplate(CloneStructureViewModel model, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto antes de clonar una plantilla.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        if (!ModelState.IsValid)
        {
            var templates = await _executor.SendOrThrowAsync(
                new ListKnowledgeStructureTemplatesQuery(IncludeArchived: false), ct);
            ViewBag.Templates = templates;
            return View("CloneFromTemplate", model);
        }

        var incubatorId = User.GetActiveIncubatorId();
        var result = await _executor.SendAndLogIfFailureAsync(
            new CloneKnowledgeStructureTemplateCommand(
                model.SourceTemplateExternalId,
                projectId.Value,
                incubatorId), ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Estructura de conocimiento clonada exitosamente.";
            return RedirectToAction(nameof(ProjectStructureDetail), new { externalId = result.Value });
        }

        ModelState.AddModelError(string.Empty, FirstErrorMessage(result) ?? "Error al clonar la plantilla.");
        var reloadTemplates = await _executor.SendOrThrowAsync(
            new ListKnowledgeStructureTemplatesQuery(IncludeArchived: false), ct);
        ViewBag.Templates = reloadTemplates;
        return View("CloneFromTemplate", model);
    }

    // -------------------------------------------------------------------------
    // Structure root (clone)
    // -------------------------------------------------------------------------
    [HttpPost("Projects/{structureExternalId:guid}/Update")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> UpdateProjectStructure(Guid structureExternalId, [FromBody] UpdateTemplateViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateKnowledgeStructureCommand(structureExternalId, model.Name, model.Description), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/SetSyncMode")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> SetProjectStructureSyncMode(Guid structureExternalId, [FromBody] SetSyncModeInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new SetSyncModeCommand(structureExternalId, input.SyncMode), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/SyncFromTemplate")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> SyncFromTemplate(Guid structureExternalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new SyncFromTemplateCommand(structureExternalId), ct);

        return result.IsSuccess ? JsonSuccess(result.Value!) : JsonFailure(result);
    }

    // -------------------------------------------------------------------------
    // Modules (clone)
    // -------------------------------------------------------------------------
    [HttpPost("Projects/{structureExternalId:guid}/Modules/Add")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> AddProjectModule(Guid structureExternalId, [FromBody] AddModuleInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new AddModuleCommand(structureExternalId, input.Name, input.Description, input.SortOrder), ct);

        return result.IsSuccess
            ? JsonSuccess(new { externalId = result.Value })
            : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Modules/{moduleExternalId:guid}/Update")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> UpdateProjectModule(Guid structureExternalId, Guid moduleExternalId, [FromBody] UpdateModuleInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateModuleCommand(structureExternalId, moduleExternalId, input.Name, input.Description), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Modules/{moduleExternalId:guid}/Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> DeleteProjectModule(Guid structureExternalId, Guid moduleExternalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new DeleteModuleCommand(structureExternalId, moduleExternalId), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Modules/Reorder")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> ReorderProjectModules(Guid structureExternalId, [FromBody] ReorderInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new ReorderModulesCommand(structureExternalId, input.ExternalIds), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    // -------------------------------------------------------------------------
    // Topics (clone)
    // -------------------------------------------------------------------------
    [HttpPost("Projects/{structureExternalId:guid}/Topics/Add")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> AddProjectTopic(Guid structureExternalId, [FromBody] AddTopicInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new AddTopicCommand(structureExternalId, input.ModuleExternalId, input.Name, input.Description, input.SortOrder), ct);

        return result.IsSuccess
            ? JsonSuccess(new { externalId = result.Value })
            : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Topics/{topicExternalId:guid}/Update")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> UpdateProjectTopic(Guid structureExternalId, Guid topicExternalId, [FromBody] UpdateTopicInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateTopicCommand(structureExternalId, topicExternalId, input.Name, input.Description), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Topics/{topicExternalId:guid}/Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> DeleteProjectTopic(Guid structureExternalId, Guid topicExternalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new DeleteTopicCommand(structureExternalId, topicExternalId), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Modules/{moduleExternalId:guid}/Topics/Reorder")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> ReorderProjectTopics(Guid structureExternalId, Guid moduleExternalId, [FromBody] ReorderInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new ReorderTopicsCommand(structureExternalId, moduleExternalId, input.ExternalIds), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Topics/{topicExternalId:guid}/PriorityRanges")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> UpdateProjectTopicPriorityRanges(Guid structureExternalId, Guid topicExternalId, [FromBody] UpdateTopicPriorityRangesInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateTopicPriorityRangesCommand(
                structureExternalId,
                topicExternalId,
                ToClonePriorityRange(input.High),
                ToClonePriorityRange(input.Medium),
                ToClonePriorityRange(input.Low)), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    // -------------------------------------------------------------------------
    // Subjects (clone)
    // -------------------------------------------------------------------------
    [HttpPost("Projects/{structureExternalId:guid}/Subjects/Add")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> AddProjectSubject(Guid structureExternalId, [FromBody] AddSubjectInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new AddSubjectCommand(structureExternalId, input.TopicExternalId, input.Name, input.Description, input.SortOrder), ct);

        return result.IsSuccess
            ? JsonSuccess(new { externalId = result.Value })
            : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Subjects/{subjectExternalId:guid}/Update")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> UpdateProjectSubject(Guid structureExternalId, Guid subjectExternalId, [FromBody] UpdateSubjectInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateSubjectCommand(structureExternalId, subjectExternalId, input.Name, input.Description), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Subjects/{subjectExternalId:guid}/Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> DeleteProjectSubject(Guid structureExternalId, Guid subjectExternalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new DeleteSubjectCommand(structureExternalId, subjectExternalId), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Topics/{topicExternalId:guid}/Subjects/Reorder")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> ReorderProjectSubjects(Guid structureExternalId, Guid topicExternalId, [FromBody] ReorderInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new ReorderSubjectsCommand(structureExternalId, topicExternalId, input.ExternalIds), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    // -------------------------------------------------------------------------
    // Resources (clone)
    // -------------------------------------------------------------------------
    [HttpPost("Projects/{structureExternalId:guid}/Resources/Add")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> AddProjectResource(Guid structureExternalId, [FromBody] AddResourceInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new AddResourceCommand(
                structureExternalId,
                input.SubjectExternalId,
                input.Title,
                input.Description,
                input.Url,
                input.ResourceType,
                input.SortOrder), ct);

        return result.IsSuccess
            ? JsonSuccess(new { externalId = result.Value })
            : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Resources/{resourceExternalId:guid}/Update")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> UpdateProjectResource(Guid structureExternalId, Guid resourceExternalId, [FromBody] UpdateResourceInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateResourceCommand(
                structureExternalId,
                resourceExternalId,
                input.Title,
                input.Description,
                input.Url,
                input.ResourceType), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Resources/{resourceExternalId:guid}/Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> DeleteProjectResource(Guid structureExternalId, Guid resourceExternalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new DeleteResourceCommand(structureExternalId, resourceExternalId), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    [HttpPost("Projects/{structureExternalId:guid}/Subjects/{subjectExternalId:guid}/Resources/Reorder")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
    public async Task<IActionResult> ReorderProjectResources(Guid structureExternalId, Guid subjectExternalId, [FromBody] ReorderInputModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return JsonValidationFailure();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new ReorderResourcesCommand(structureExternalId, subjectExternalId, input.ExternalIds), ct);

        return result.IsSuccess ? JsonSuccess() : JsonFailure(result);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------
    private static Mentoory.Knowledge.Application.Commands.UpdateTopicTemplatePriorityRanges.PriorityRangeInput? ToPriorityRange(PriorityRangeViewModel? vm)
    {
        if (vm is null || !vm.Min.HasValue || !vm.Max.HasValue)
        {
            return null;
        }

        return new Mentoory.Knowledge.Application.Commands.UpdateTopicTemplatePriorityRanges.PriorityRangeInput(vm.Min.Value, vm.Max.Value);
    }

    private static Mentoory.Knowledge.Application.Commands.UpdateTopicPriorityRanges.PriorityRangeInput? ToClonePriorityRange(PriorityRangeViewModel? vm)
    {
        if (vm is null || !vm.Min.HasValue || !vm.Max.HasValue)
        {
            return null;
        }

        return new Mentoory.Knowledge.Application.Commands.UpdateTopicPriorityRanges.PriorityRangeInput(vm.Min.Value, vm.Max.Value);
    }

    private static string? FirstErrorMessage(Result result) =>
        result.ErrorMessages is { Length: > 0 } messages ? messages[0].Message : null;

    private IActionResult JsonSuccess() => Json(new { success = true });

    private IActionResult JsonSuccess(object data) => Json(new { success = true, data });

    private IActionResult JsonFailure(Result result) =>
        Json(new { success = false, message = FirstErrorMessage(result) ?? "Error desconocido." });

    private IActionResult JsonValidationFailure()
    {
        var message = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
            ?? "Los datos ingresados no son válidos.";

        return Json(new { success = false, message });
    }
}
