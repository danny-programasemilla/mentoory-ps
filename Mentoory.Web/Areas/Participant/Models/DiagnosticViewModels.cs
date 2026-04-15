using System.ComponentModel.DataAnnotations;

namespace Mentoory.Web.Areas.Participant.Models;

public sealed class DiagnosticFormViewModel
{
    public Guid FormExternalId { get; set; }
    public Guid StageFormAssignmentExternalId { get; set; }
    public string FormName { get; set; } = null!;
    public List<QuestionViewModel> Questions { get; set; } = new();
}

public sealed class QuestionViewModel
{
    public long QuestionId { get; set; }
    public Guid QuestionExternalId { get; set; }
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public bool IsOptional { get; set; }
    public string? BlockGroup { get; set; }
    public List<AnswerOptionViewModel> AnswerOptions { get; set; } = new();
    public List<FollowUpQuestionViewModel> FollowUpQuestions { get; set; } = new();
}

public sealed class AnswerOptionViewModel
{
    public long AnswerOptionId { get; set; }
    public string OptionText { get; set; } = null!;
    public int SortOrder { get; set; }
}

public sealed class FollowUpQuestionViewModel
{
    public long FollowUpQuestionId { get; set; }
    public string QuestionText { get; set; } = null!;
}

public sealed class SubmitDiagnosticViewModel
{
    public Guid FormExternalId { get; set; }

    [Display(Name = "Asignación de Formulario")]
    [Required(ErrorMessage = "La asignación de formulario es requerida.")]
    public Guid StageFormAssignmentExternalId { get; set; }

    public List<ResponseItemViewModel> Responses { get; set; } = new();
}

public sealed class ResponseItemViewModel
{
    public long QuestionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public List<long>? SelectedOptionIds { get; set; }
}

public sealed class DiagnosticLandingViewModel
{
    public List<DiagnosticStageFormViewModel> Assignments { get; set; } = new();
}

public sealed class DiagnosticStageFormViewModel
{
    public Guid AssignmentExternalId { get; set; }
    public long ProjectStageId { get; set; }
    public string FormName { get; set; } = null!;
    public int QuestionCount { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
