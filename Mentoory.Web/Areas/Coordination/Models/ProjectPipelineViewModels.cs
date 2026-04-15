using System.ComponentModel.DataAnnotations;
using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Web.Areas.Coordination.Models;

public sealed class PipelineViewModel
{
    public Guid ProjectExternalId { get; set; }
    public string ProjectName { get; set; } = null!;
    public string CurrentStageState { get; set; } = null!;
    public List<StageViewModel> Stages { get; set; } = new();
}

public sealed class StageViewModel
{
    public Guid ExternalId { get; set; }
    public string StageType { get; set; } = null!;
    public string State { get; set; } = null!;
    public int Position { get; set; }
    public string DisplayName { get; set; } = null!;
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class AddStageViewModel
{
    [Display(Name = "Tipo de etapa")]
    [Required(ErrorMessage = "El tipo de etapa es requerido.")]
    public StageType StageType { get; set; }

    [Display(Name = "Posición")]
    [Required(ErrorMessage = "La posición es requerida.")]
    [Range(1, 100, ErrorMessage = "La posición debe ser un valor válido.")]
    public int Position { get; set; }
}

public sealed class RenameStageViewModel
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres.")]
    public string DisplayName { get; set; } = null!;
}
