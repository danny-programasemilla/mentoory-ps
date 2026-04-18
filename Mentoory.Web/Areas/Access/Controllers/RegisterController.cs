using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Application.Countries.Queries.ListCountries;
using Mentoory.Shared.Application;
using Mentoory.Web.Areas.Access.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Mentoory.Web.Areas.Access.Controllers;

[Area("Access")]
[AllowAnonymous]
public partial class RegisterController : Controller
{
    internal const string GenericFailureViewDataKey = "GenericRegistrationError";

    private readonly MediatRExecutor _executor;
    private readonly ILogger<RegisterController> _logger;

    public RegisterController(MediatRExecutor executor, ILogger<RegisterController> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = new RegisterViewModel();
        await PopulateCountriesAsync(model, ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("registration")]
    public async Task<IActionResult> Index(RegisterViewModel model, CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        if (!ModelState.IsValid)
        {
            var firstInvalidProperty = ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .Select(kvp => kvp.Key)
                .FirstOrDefault() ?? string.Empty;

            LogValidatorFailure(model.Email, firstInvalidProperty, correlationId, clientIp);

            return await GenericFailureViewAsync(model, ct);
        }

        var command = new RegisterUserCommand(
            model.Email,
            model.Country,
            model.NationalId,
            model.FirstName,
            model.LastName,
            model.Password,
            correlationId,
            clientIp);

        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsFailure)
        {
            if (result.ErrorCode == ResultErrorCodes.Validation_SomeFieldsAreInvalid)
            {
                var failingProperty = result.ErrorMessages?.FirstOrDefault().Context ?? string.Empty;
                LogValidatorFailure(model.Email, failingProperty, correlationId, clientIp);
            }

            return await GenericFailureViewAsync(model, ct);
        }

        return RedirectToAction(nameof(Success));
    }

    [HttpGet]
    public IActionResult Success()
    {
        return View();
    }

    private async Task<IActionResult> GenericFailureViewAsync(RegisterViewModel model, CancellationToken ct)
    {
        ModelState.Clear();
        ViewData[GenericFailureViewDataKey] = true;
        await PopulateCountriesAsync(model, ct);
        return View(nameof(Index), model);
    }

    private async Task PopulateCountriesAsync(RegisterViewModel model, CancellationToken ct)
    {
        var countriesResult = await _executor.SendAndLogIfFailureAsync(new ListCountriesQuery(), ct);
        model.Countries = countriesResult.IsSuccess ? countriesResult.Value! : [];
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Public registration outcome. Email: {Email}, Outcome: ValidatorFailure:{Rule}, CorrelationId: {CorrelationId}, ClientIp: {ClientIp}")]
    partial void LogValidatorFailure(string email, string rule, string correlationId, string clientIp);
}
