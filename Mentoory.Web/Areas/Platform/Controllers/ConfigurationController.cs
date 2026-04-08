using Mentoory.Access.Application.Configuration.Commands.UpdateConfiguration;
using Mentoory.Access.Application.Configuration.Queries.ListAllConfigurations;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Platform.Controllers;

/// <summary>
/// Controller for managing system configuration settings.
/// </summary>
[Area("Platform")]
[Route("[area]/[controller]")]
[Authorize(Roles = "GlobalAdmin")]
public class ConfigurationController : Controller
{
    private readonly MediatRExecutor _executor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
    /// </summary>
    /// <param name="executor">The MediatR executor for sending commands and queries.</param>
    public ConfigurationController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Displays the list of all system configuration settings.
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var configs = await _executor.SendOrThrowAsync(new ListAllConfigurationsQuery(), ct);
        return View(configs);
    }

    /// <summary>
    /// Updates a system configuration value.
    /// </summary>
    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string key, string value, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateConfigurationCommand(key, value), ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Configuración actualizada exitosamente.";
        }
        else
        {
            var errorMessage = result.ErrorMessages?.Length > 0
                ? string.Join(", ", result.ErrorMessages.Select(m => m.Message))
                : "Error al actualizar la configuración.";
            TempData["ErrorMessage"] = errorMessage;
        }

        return RedirectToAction(nameof(Index));
    }
}
