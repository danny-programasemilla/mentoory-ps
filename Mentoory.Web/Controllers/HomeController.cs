using System.Diagnostics;
using Mentoory.Example.Application.EntityExample.Queries.GetAllRecords;
using Microsoft.AspNetCore.Mvc;
using Mentoory.Web.Models;
using Mentoory.Web.Services;

namespace Mentoory.Web.Controllers;

public class HomeController(MediatRExecutor mediatRExecutor) : Controller
{
    private readonly MediatRExecutor _mediatRExecutor = mediatRExecutor;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var records = await _mediatRExecutor.SendOrThrowAsync(new GetAllRecordsQuery(), cancellationToken)
            .ConfigureAwait(false);

        return View(records);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
