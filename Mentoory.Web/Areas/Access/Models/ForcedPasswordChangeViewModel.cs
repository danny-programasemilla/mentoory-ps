using System.ComponentModel.DataAnnotations;

namespace Mentoory.Web.Areas.Access.Models;

public class ForcedPasswordChangeViewModel
{
    [Required(ErrorMessage = "La contraseña actual es requerida.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    [MinLength(12, ErrorMessage = "La contraseña debe tener al menos 12 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nueva contraseña")]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
