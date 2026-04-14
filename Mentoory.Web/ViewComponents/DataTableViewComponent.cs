using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.ViewComponents;

public class DataTableViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(
        string tableId,
        string apiUrl,
        object[] columns,
        string? filterId = null,
        string? title = null,
        string? emptyIcon = null,
        string? emptyTitle = null,
        string? emptyMessage = null,
        string? emptyActionUrl = null,
        string? emptyActionText = null)
    {
        ViewBag.TableId = tableId;
        ViewBag.ApiUrl = apiUrl;
        ViewBag.Columns = columns;
        ViewBag.FilterId = filterId;
        ViewBag.Title = title;
        ViewBag.EmptyIcon = emptyIcon;
        ViewBag.EmptyTitle = emptyTitle;
        ViewBag.EmptyMessage = emptyMessage;
        ViewBag.EmptyActionUrl = emptyActionUrl;
        ViewBag.EmptyActionText = emptyActionText;
        return View();
    }
}
