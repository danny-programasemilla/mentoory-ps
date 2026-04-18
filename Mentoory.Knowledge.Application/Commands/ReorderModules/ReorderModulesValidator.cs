using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.ReorderModules;

/// <summary>
/// Validator for the <see cref="ReorderModulesCommand"/>.
/// </summary>
public class ReorderModulesValidator : AbstractValidator<ReorderModulesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderModulesValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public ReorderModulesValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.ModuleExternalIdsInOrder)
            .NotEmpty().WithMessage("La lista de módulos es requerida.")
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage("No se permiten módulos duplicados en la lista.");
    }
}
