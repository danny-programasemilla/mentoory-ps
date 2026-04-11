using System.ComponentModel.DataAnnotations;

namespace Mentoory.Web.Areas.Coordination.Models;

public sealed class CloneDiagnosticFormViewModel
{
    [Display(Name = "Plantilla")]
    [Required(ErrorMessage = "Debe seleccionar una plantilla.")]
    public Guid SourceTemplateExternalId { get; set; }
}

public sealed class FormTemplateOptionViewModel
{
    public Guid ExternalId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Version { get; set; }
}
