using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Application.Countries.Queries.ListCountries;
using Mentoory.Web.Areas.Access.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Mentoory.Web.Areas.Access.Controllers;

[Area("Access")]
[AllowAnonymous]
public class RegisterController : Controller
{
    private readonly MediatRExecutor _executor;

    public RegisterController(MediatRExecutor executor)
    {
        _executor = executor;
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
        if (!ModelState.IsValid)
        {
            await PopulateCountriesAsync(model, ct);
            return View(model);
        }

        var command = new RegisterUserCommand(
            model.Email,
            model.Country,
            model.NationalId,
            model.FirstName,
            model.LastName,
            model.Password);

        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsFailure)
        {
            foreach (var error in result.ErrorMessages ?? [])
            {
                ModelState.AddModelError(error.Context, error.Message);
            }

            await PopulateCountriesAsync(model, ct);
            return View(model);
        }

        return RedirectToAction("Success");
    }

    [HttpGet]
    public IActionResult Success()
    {
        return View();
    }

    private async Task PopulateCountriesAsync(RegisterViewModel model, CancellationToken ct)
    {
        var countriesResult = await _executor.SendAndLogIfFailureAsync(new ListCountriesQuery(), ct);
        model.Countries = countriesResult.IsSuccess ? countriesResult.Value! : [];
    }
}
