using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.ReorderTopics;

/// <summary>
/// Validator for the <see cref="ReorderTopicsCommand"/>.
/// </summary>
public class ReorderTopicsValidator : AbstractValidator<ReorderTopicsCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderTopicsValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public ReorderTopicsValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.ModuleExternalId)
            .NotEmpty().WithMessage("El identificador del módulo es requerido.");

        RuleFor(x => x.TopicExternalIdsInOrder)
            .NotEmpty().WithMessage("La lista de temas es requerida.")
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage("No se permiten temas duplicados en la lista.");
    }
}
