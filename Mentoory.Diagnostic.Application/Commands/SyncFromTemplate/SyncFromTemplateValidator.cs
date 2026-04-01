using FluentValidation;

namespace Mentoory.Diagnostic.Application.Commands.SyncFromTemplate;

/// <summary>
/// Validator for the <see cref="SyncFromTemplateCommand"/>.
/// </summary>
public class SyncFromTemplateValidator : AbstractValidator<SyncFromTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncFromTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public SyncFromTemplateValidator()
    {
        RuleFor(x => x.ProjectFormExternalId)
            .NotEmpty().WithMessage("El identificador del formulario del proyecto es requerido.");
    }
}
