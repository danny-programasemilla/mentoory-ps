using System.ComponentModel.DataAnnotations;
using Mentoory.Access.Application.Countries.Queries.ListCountries;

namespace Mentoory.Web.Areas.Administration.Models;

public class CreateUserViewModel
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

    [Required(ErrorMessage = "El nombre es requerido.")]
    [Display(Name = "Nombre")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido.")]
    [Display(Name = "Apellido")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Omitir verificación de correo")]
    public bool SkipEmailVerification { get; set; }

    [Display(Name = "Omitir aceptación de invitación")]
    public bool SkipInvitationAcceptance { get; set; }

    public List<CountryDto> Countries { get; set; } = [];

    public string? ActiveProjectName { get; set; }

    public bool HasActiveProject { get; set; }
}
