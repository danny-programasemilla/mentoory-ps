using FluentValidation;

namespace Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;

/// <summary>
/// Validator for the <see cref="CloneFormTemplateCommand"/>.
/// </summary>
public class CloneFormTemplateValidator : AbstractValidator<CloneFormTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloneFormTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public CloneFormTemplateValidator()
    {
        RuleFor(x => x.SourceTemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla de origen es requerido.");

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("El identificador del proyecto es requerido.");

        RuleFor(x => x.IncubatorId)
            .GreaterThan(0).WithMessage("El identificador de la incubadora es requerido.");
    }
}
