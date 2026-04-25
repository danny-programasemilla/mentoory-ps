using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.UpdateModule;

/// <summary>
/// Validator for the <see cref="UpdateModuleCommand"/>.
/// </summary>
public class UpdateModuleValidator : AbstractValidator<UpdateModuleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateModuleValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public UpdateModuleValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.ModuleExternalId)
            .NotEmpty().WithMessage("El identificador del módulo es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción no puede exceder 2000 caracteres.")
            .When(x => x.Description is not null);
    }
}
