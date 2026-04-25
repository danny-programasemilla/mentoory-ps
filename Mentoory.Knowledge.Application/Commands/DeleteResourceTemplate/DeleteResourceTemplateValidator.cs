using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.DeleteResourceTemplate;

/// <summary>
/// Validator for the <see cref="DeleteResourceTemplateCommand"/>.
/// </summary>
public class DeleteResourceTemplateValidator : AbstractValidator<DeleteResourceTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteResourceTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public DeleteResourceTemplateValidator()
    {
        RuleFor(x => x.TemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla es requerido.");

        RuleFor(x => x.ResourceExternalId)
            .NotEmpty().WithMessage("El identificador del recurso es requerido.");
    }
}
