using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.ViewComponents;

public class ToastViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
