using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.ViewComponents;

public class DataTableViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(
        string tableId,
        string[] columns,
        string? title = null)
    {
        ViewBag.TableId = tableId;
        ViewBag.Columns = columns;
        ViewBag.Title = title;
        return View();
    }
}
