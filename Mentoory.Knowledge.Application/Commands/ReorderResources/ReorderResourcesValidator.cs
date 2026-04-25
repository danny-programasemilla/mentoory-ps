using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.ReorderResources;

/// <summary>
/// Validator for the <see cref="ReorderResourcesCommand"/>.
/// </summary>
public class ReorderResourcesValidator : AbstractValidator<ReorderResourcesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderResourcesValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public ReorderResourcesValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.SubjectExternalId)
            .NotEmpty().WithMessage("El identificador de la materia es requerido.");

        RuleFor(x => x.ResourceExternalIdsInOrder)
            .NotEmpty().WithMessage("La lista de recursos es requerida.")
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage("No se permiten recursos duplicados en la lista.");
    }
}
