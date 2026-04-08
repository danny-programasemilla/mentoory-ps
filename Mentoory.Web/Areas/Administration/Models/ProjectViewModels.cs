using System.ComponentModel.DataAnnotations;

namespace Mentoory.Web.Areas.Administration.Models;

public sealed class CreateProjectViewModel
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres.")]
    [Display(Name = "Nombre del Proyecto")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [Display(Name = "Visible públicamente")]
    public bool IsPublic { get; set; }

    [Display(Name = "Variante de inscripción")]
    public int EnrollmentVariant { get; set; }
}
