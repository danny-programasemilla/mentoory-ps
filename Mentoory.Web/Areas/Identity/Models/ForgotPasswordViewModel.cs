using System.ComponentModel.DataAnnotations;

namespace Mentoory.Web.Areas.Identity.Models;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;
}
