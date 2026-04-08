using System.ComponentModel.DataAnnotations;
using Mentoory.Access.Application.Countries.Queries.ListCountries;

namespace Mentoory.Web.Areas.Administration.Models;

public class InternalRegistrationViewModel
{
    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El país es requerido.")]
    [Display(Name = "País")]
    public string Country { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de identificación es requerido.")]
    [Display(Name = "Identificación")]
    public string Identification { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    [MinLength(12, ErrorMessage = "La contraseña debe tener al menos 12 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Requiere verificación de correo")]
    public bool RequireEmailVerification { get; set; }

    [Required(ErrorMessage = "El proyecto es requerido.")]
    [Display(Name = "Proyecto")]
    public Guid ProjectExternalId { get; set; }

    public List<CountryDto> Countries { get; set; } = [];
}
