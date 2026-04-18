using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.DeleteModule;

/// <summary>
/// Validator for the <see cref="DeleteModuleCommand"/>.
/// </summary>
public class DeleteModuleValidator : AbstractValidator<DeleteModuleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteModuleValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public DeleteModuleValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.ModuleExternalId)
            .NotEmpty().WithMessage("El identificador del módulo es requerido.");
    }
}
