using System.ComponentModel.DataAnnotations;

namespace Mentoory.Web.Areas.Administration.Models;

public sealed class EnrollUserViewModel
{
    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El país es requerido.")]
    [StringLength(3, ErrorMessage = "El código de país no puede exceder 3 caracteres.")]
    [Display(Name = "País")]
    public string Country { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de identificación es requerido.")]
    [StringLength(50, ErrorMessage = "El número de identificación no puede exceder 50 caracteres.")]
    [Display(Name = "Número de Identificación")]
    public string NationalId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es requerido.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido.")]
    [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres.")]
    [Display(Name = "Apellido")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
