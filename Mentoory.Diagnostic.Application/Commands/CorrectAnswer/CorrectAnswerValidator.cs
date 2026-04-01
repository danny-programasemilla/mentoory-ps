using FluentValidation;

namespace Mentoory.Diagnostic.Application.Commands.CorrectAnswer;

/// <summary>
/// Validator for the <see cref="CorrectAnswerCommand"/>.
/// </summary>
public class CorrectAnswerValidator : AbstractValidator<CorrectAnswerCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CorrectAnswerValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public CorrectAnswerValidator()
    {
        RuleFor(x => x.DiagnosticResponseExternalId)
            .NotEmpty().WithMessage("El identificador de la respuesta diagnóstica es requerido.");

        RuleFor(x => x.QuestionResponseId)
            .GreaterThan(0).WithMessage("El identificador de la respuesta a la pregunta es requerido.");

        RuleFor(x => x.CorrectedByUserId)
            .GreaterThan(0).WithMessage("El identificador del usuario que realiza la corrección es requerido.");

        RuleFor(x => x)
            .Must(x => x.NewTextValue is not null || x.NewNumericValue is not null || x.NewSelectedOptionIds is not null)
            .WithMessage("Debe proporcionar al menos un valor corregido (texto, numérico u opciones seleccionadas).");
    }
}
