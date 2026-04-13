using System.ComponentModel.DataAnnotations;

namespace Mentoory.Web.Areas.Administration.Models;

public class BatchUploadViewModel
{
    [Required(ErrorMessage = "El archivo CSV es requerido.")]
    [Display(Name = "Archivo CSV")]
    public IFormFile? CsvFile { get; set; }

    [Display(Name = "Omitir verificación de correo")]
    public bool SkipEmailVerification { get; set; }

    [Display(Name = "Omitir aceptación de invitación")]
    public bool SkipInvitationAcceptance { get; set; }

    public bool HasActiveProject { get; set; }

    public string? ActiveProjectName { get; set; }
}
