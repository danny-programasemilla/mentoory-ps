using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.UpdateModuleTemplate;

/// <summary>
/// Validator for the <see cref="UpdateModuleTemplateCommand"/>.
/// </summary>
public class UpdateModuleTemplateValidator : AbstractValidator<UpdateModuleTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateModuleTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public UpdateModuleTemplateValidator()
    {
        RuleFor(x => x.TemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla es requerido.");

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
