using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.ReorderSubjects;

/// <summary>
/// Validator for the <see cref="ReorderSubjectsCommand"/>.
/// </summary>
public class ReorderSubjectsValidator : AbstractValidator<ReorderSubjectsCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderSubjectsValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public ReorderSubjectsValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.TopicExternalId)
            .NotEmpty().WithMessage("El identificador del tema es requerido.");

        RuleFor(x => x.SubjectExternalIdsInOrder)
            .NotEmpty().WithMessage("La lista de materias es requerida.")
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage("No se permiten materias duplicadas en la lista.");
    }
}
