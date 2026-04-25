using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.DeleteModuleTemplate;

/// <summary>
/// Validator for the <see cref="DeleteModuleTemplateCommand"/>.
/// </summary>
public class DeleteModuleTemplateValidator : AbstractValidator<DeleteModuleTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteModuleTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public DeleteModuleTemplateValidator()
    {
        RuleFor(x => x.TemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla es requerido.");

        RuleFor(x => x.ModuleExternalId)
            .NotEmpty().WithMessage("El identificador del módulo es requerido.");
    }
}
