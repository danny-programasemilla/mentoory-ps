using System.ComponentModel.DataAnnotations;

namespace Mentoory.Web.Areas.Platform.Models;

public sealed class CreateIncubatorViewModel
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }
}

public sealed class EditIncubatorViewModel
{
    public Guid ExternalId { get; set; }

    [Required(ErrorMessage = "El nombre es requerido.")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [Display(Name = "Estado")]
    public bool IsActive { get; set; }
}
