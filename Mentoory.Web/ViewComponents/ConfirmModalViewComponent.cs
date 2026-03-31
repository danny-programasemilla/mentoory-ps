using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.ViewComponents;

public class ConfirmModalViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string modalId = "confirmModal", string title = "Confirmar", string message = "¿Está seguro?")
    {
        ViewBag.ModalId = modalId;
        ViewBag.Title = title;
        ViewBag.Message = message;
        return View();
    }
}
