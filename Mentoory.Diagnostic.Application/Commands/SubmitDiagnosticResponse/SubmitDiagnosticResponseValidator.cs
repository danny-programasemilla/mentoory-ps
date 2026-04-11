using FluentValidation;

namespace Mentoory.Diagnostic.Application.Commands.SubmitDiagnosticResponse;

/// <summary>
/// Validator for the <see cref="SubmitDiagnosticResponseCommand"/>.
/// </summary>
public class SubmitDiagnosticResponseValidator : AbstractValidator<SubmitDiagnosticResponseCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitDiagnosticResponseValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public SubmitDiagnosticResponseValidator()
    {
        RuleFor(x => x.ProjectFormExternalId)
            .NotEmpty().WithMessage("El identificador del formulario del proyecto es requerido.");

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("El identificador del proyecto es requerido.");

        RuleFor(x => x.IncubatorId)
            .GreaterThan(0).WithMessage("El identificador de la incubadora es requerido.");

        RuleFor(x => x.EntrepreneurUserId)
            .GreaterThan(0).WithMessage("El identificador del usuario emprendedor es requerido.");

        RuleFor(x => x.EvaluationStage)
            .IsInEnum().WithMessage("La etapa de evaluación no es válida.");

        RuleFor(x => x.Responses)
            .NotEmpty().WithMessage("Las respuestas son requeridas.");

        RuleForEach(x => x.Responses).ChildRules(response =>
        {
            response.RuleFor(r => r.QuestionId)
                .GreaterThan(0).WithMessage("El identificador de la pregunta es requerido.");
        });
    }
}
