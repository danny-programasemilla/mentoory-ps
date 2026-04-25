using Mentoory.Access.Application.StageActions;
using Mentoory.Tenant.Application.Projects.Queries.GetProjectCurrentStage;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Mentoory.Web.Infrastructure.Filters;

/// <summary>
/// Enforces server-side stage gating for coordination-area actions. Resolves the target
/// project from the <c>projectExternalId</c> route value first, then falls back to the
/// session-active project id claim.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequiresStageAttribute(StageGatedAction action) : Attribute, IAsyncActionFilter
{
    public StageGatedAction Action { get; } = action;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var executor = services.GetRequiredService<MediatRExecutor>();

        var externalId = ResolveExternalIdFromRoute(context);
        var internalId = externalId is null ? context.HttpContext.User.GetActiveProjectId() : null;

        if (externalId is null && internalId is null)
        {
            SetWarning(context, "No se pudo determinar el proyecto para validar la etapa.");
            context.Result = RedirectToLifecycleFallback();
            return;
        }

        var query = new GetProjectCurrentStageQuery(externalId, internalId);
        var result = await executor.SendAndLogIfFailureAsync(query, context.HttpContext.RequestAborted);

        if (result.IsFailure || result.Value is null)
        {
            SetWarning(context, "No se pudo determinar la etapa actual del proyecto.");
            context.Result = RedirectToLifecycleFallback();
            return;
        }

        var state = StageActionRegistry.GetState(result.Value.CurrentStageType, Action);
        if (state == StageGatedActionState.Available)
        {
            await next();
            return;
        }

        var gatingStageDisplay = StageTypeDisplay.ToSpanish(StageActionRegistry.GetGatingStage(Action));
        SetWarning(context, $"Esta acción estará disponible desde la etapa {gatingStageDisplay}.");
        context.Result = new RedirectToActionResult(
            actionName: "Lifecycle",
            controllerName: "Projects",
            routeValues: new { area = "Coordination", externalId = result.Value.ProjectExternalId });
    }

    private static Guid? ResolveExternalIdFromRoute(ActionExecutingContext context)
    {
        if (context.RouteData.Values.TryGetValue("projectExternalId", out var raw)
            && raw is not null
            && Guid.TryParse(raw.ToString(), out var parsed)
            && parsed != Guid.Empty)
        {
            return parsed;
        }

        return null;
    }

    private static void SetWarning(ActionExecutingContext context, string message)
    {
        if (context.Controller is Controller controller)
        {
            controller.TempData[TempDataKeys.WarningMessage] = message;
            return;
        }

        var tempDataFactory = context.HttpContext.RequestServices
            .GetRequiredService<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>();
        var tempData = tempDataFactory.GetTempData(context.HttpContext);
        tempData[TempDataKeys.WarningMessage] = message;
        tempData.Save();
    }

    private static IActionResult RedirectToLifecycleFallback()
    {
        return new RedirectToActionResult(
            actionName: "Index",
            controllerName: "Projects",
            routeValues: new { area = "Coordination" });
    }
}
