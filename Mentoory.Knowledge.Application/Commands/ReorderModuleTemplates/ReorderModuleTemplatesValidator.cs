using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.ReorderModuleTemplates;

/// <summary>
/// Validator for the <see cref="ReorderModuleTemplatesCommand"/>.
/// </summary>
public class ReorderModuleTemplatesValidator : AbstractValidator<ReorderModuleTemplatesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderModuleTemplatesValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public ReorderModuleTemplatesValidator()
    {
        RuleFor(x => x.TemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla es requerido.");

        RuleFor(x => x.ModuleExternalIdsInOrder)
            .NotEmpty().WithMessage("La lista de módulos es requerida.")
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage("No se permiten módulos duplicados en la lista.");
    }
}
