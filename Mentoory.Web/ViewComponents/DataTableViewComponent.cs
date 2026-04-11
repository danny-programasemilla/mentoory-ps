using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.ViewComponents;

public class DataTableViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string tableId, string apiUrl, object[] columns, string? filterId = null)
    {
        ViewBag.TableId = tableId;
        ViewBag.ApiUrl = apiUrl;
        ViewBag.Columns = columns;
        ViewBag.FilterId = filterId;
        return View();
    }
}
