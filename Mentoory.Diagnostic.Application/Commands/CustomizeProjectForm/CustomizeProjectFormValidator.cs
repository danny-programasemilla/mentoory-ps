using FluentValidation;

namespace Mentoory.Diagnostic.Application.Commands.CustomizeProjectForm;

/// <summary>
/// Validator for the <see cref="CustomizeProjectFormCommand"/>.
/// </summary>
public class CustomizeProjectFormValidator : AbstractValidator<CustomizeProjectFormCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomizeProjectFormValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public CustomizeProjectFormValidator()
    {
        RuleFor(x => x.ProjectFormExternalId)
            .NotEmpty().WithMessage("El identificador del formulario del proyecto es requerido.");

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("La acción debe ser 'AddQuestion', 'RemoveQuestion' o 'ReorderQuestions'.");

        When(x => x.Action == CustomizeAction.AddQuestion, () =>
        {
            RuleFor(x => x.QuestionData)
                .NotNull().WithMessage("Los datos de la pregunta son requeridos para agregar una pregunta.");

            When(x => x.QuestionData is not null, () =>
            {
                RuleFor(x => x.QuestionData!.QuestionText)
                    .NotEmpty().WithMessage("El texto de la pregunta es requerido.");

                RuleFor(x => x.QuestionData!.TopicId)
                    .GreaterThan(0).WithMessage("El identificador del tema es requerido.");
            });
        });

        When(x => x.Action == CustomizeAction.RemoveQuestion, () =>
        {
            RuleFor(x => x.QuestionIdToRemove)
                .NotNull().WithMessage("El identificador de la pregunta a eliminar es requerido.")
                .GreaterThan(0).WithMessage("El identificador de la pregunta a eliminar debe ser mayor a cero.");
        });

        When(x => x.Action == CustomizeAction.ReorderQuestions, () =>
        {
            RuleFor(x => x.QuestionIdsInOrder)
                .NotNull().WithMessage("La lista de identificadores de preguntas en orden es requerida.")
                .Must(ids => ids is { Count: > 0 })
                .WithMessage("La lista de identificadores de preguntas en orden no puede estar vacía.");
        });
    }
}
