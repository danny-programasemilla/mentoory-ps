using System.ComponentModel.DataAnnotations;
using Mentoory.Knowledge.Domain.Enums;

namespace Mentoory.Web.Areas.Coordination.Models.Knowledge;

// -----------------------------------------------------------------------------
// Template-level (root) view models
// -----------------------------------------------------------------------------
public sealed class CreateTemplateViewModel
{
    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string? Description { get; set; }
}

public sealed class UpdateTemplateViewModel
{
    [Required]
    public Guid ExternalId { get; set; }

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string? Description { get; set; }
}

// -----------------------------------------------------------------------------
// Priority range editor (3 bands per topic)
// -----------------------------------------------------------------------------
public sealed class PriorityRangeViewModel
{
    [Display(Name = "Mínimo")]
    public decimal? Min { get; set; }

    [Display(Name = "Máximo")]
    public decimal? Max { get; set; }

    /// <summary>When true (either Min or Max populated), the band is considered configured.</summary>
    public bool IsConfigured => Min.HasValue && Max.HasValue;
}

public sealed class UpdateTopicPriorityRangesInputModel
{
    [Display(Name = "Rango alto")]
    public PriorityRangeViewModel? High { get; set; }

    [Display(Name = "Rango medio")]
    public PriorityRangeViewModel? Medium { get; set; }

    [Display(Name = "Rango bajo")]
    public PriorityRangeViewModel? Low { get; set; }
}

// -----------------------------------------------------------------------------
// Shared reorder input
// -----------------------------------------------------------------------------
public sealed class ReorderInputModel
{
    [Required]
    public IReadOnlyList<Guid> ExternalIds { get; set; } = Array.Empty<Guid>();
}

// -----------------------------------------------------------------------------
// Module inputs
// -----------------------------------------------------------------------------
public sealed class AddModuleInputModel
{
    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
}

public sealed class UpdateModuleInputModel
{
    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000)]
    public string? Description { get; set; }
}

// -----------------------------------------------------------------------------
// Topic inputs
// -----------------------------------------------------------------------------
public sealed class AddTopicInputModel
{
    [Required]
    public Guid ModuleExternalId { get; set; }

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
}

public sealed class UpdateTopicInputModel
{
    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000)]
    public string? Description { get; set; }
}

// -----------------------------------------------------------------------------
// Subject inputs
// -----------------------------------------------------------------------------
public sealed class AddSubjectInputModel
{
    [Required]
    public Guid TopicExternalId { get; set; }

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
}

public sealed class UpdateSubjectInputModel
{
    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000)]
    public string? Description { get; set; }
}

// -----------------------------------------------------------------------------
// Resource inputs
// -----------------------------------------------------------------------------
public sealed class AddResourceInputModel
{
    [Required]
    public Guid SubjectExternalId { get; set; }

    [Display(Name = "Título")]
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000)]
    public string? Description { get; set; }

    [Display(Name = "URL")]
    [Required(ErrorMessage = "La URL es obligatoria.")]
    [Url(ErrorMessage = "Debe ser una URL absoluta válida.")]
    [StringLength(2000)]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "Tipo de recurso")]
    public ResourceType ResourceType { get; set; } = ResourceType.Link;

    public int SortOrder { get; set; }
}

public sealed class UpdateResourceInputModel
{
    [Display(Name = "Título")]
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(2000)]
    public string? Description { get; set; }

    [Display(Name = "URL")]
    [Required(ErrorMessage = "La URL es obligatoria.")]
    [Url(ErrorMessage = "Debe ser una URL absoluta válida.")]
    [StringLength(2000)]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "Tipo de recurso")]
    public ResourceType ResourceType { get; set; } = ResourceType.Link;
}

// -----------------------------------------------------------------------------
// Project clone view models (US2)
// -----------------------------------------------------------------------------
public sealed class CloneStructureViewModel
{
    [Display(Name = "Plantilla de origen")]
    [Required(ErrorMessage = "Debe seleccionar una plantilla de origen.")]
    public Guid SourceTemplateExternalId { get; set; }
}

public sealed class SetSyncModeInputModel
{
    [Display(Name = "Modo de sincronización")]
    public SyncMode SyncMode { get; set; } = SyncMode.Disconnected;
}
