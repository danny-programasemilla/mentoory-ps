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

public sealed class FormOptionViewModel
{
    public Guid ExternalId { get; set; }
    public string Name { get; set; } = null!;
}

public sealed class StageConfigViewModel
{
    public Guid ProjectStageExternalId { get; set; }
    public List<AssignedFormViewModel> Assignments { get; set; } = new();
}

public sealed class AssignedFormViewModel
{
    public Guid ExternalId { get; set; }
    public string FormName { get; set; } = null!;
    public int QuestionCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class QuestionSelectionViewModel
{
    public Guid AssignmentExternalId { get; set; }
    public string FormName { get; set; } = null!;
    public List<SelectableQuestionViewModel> Questions { get; set; } = new();
}

public sealed class SelectableQuestionViewModel
{
    public long QuestionId { get; set; }
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public bool IsSelected { get; set; }
}
