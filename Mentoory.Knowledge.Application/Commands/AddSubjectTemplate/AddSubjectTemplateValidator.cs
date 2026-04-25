using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.AddSubjectTemplate;

/// <summary>
/// Validator for the <see cref="AddSubjectTemplateCommand"/>.
/// </summary>
public class AddSubjectTemplateValidator : AbstractValidator<AddSubjectTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddSubjectTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public AddSubjectTemplateValidator()
    {
        RuleFor(x => x.TemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla es requerido.");

        RuleFor(x => x.TopicExternalId)
            .NotEmpty().WithMessage("El identificador del tema es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción no puede exceder 2000 caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("El orden debe ser mayor o igual a cero.");
    }
}
