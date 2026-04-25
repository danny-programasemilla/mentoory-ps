using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.AddResourceTemplate;

/// <summary>
/// Validator for the <see cref="AddResourceTemplateCommand"/>.
/// </summary>
public class AddResourceTemplateValidator : AbstractValidator<AddResourceTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddResourceTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public AddResourceTemplateValidator()
    {
        RuleFor(x => x.TemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla es requerido.");

        RuleFor(x => x.SubjectExternalId)
            .NotEmpty().WithMessage("El identificador de la materia es requerido.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título es requerido.")
            .MaximumLength(200).WithMessage("El título no puede exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción no puede exceder 2000 caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("La URL es requerida.")
            .MaximumLength(2000).WithMessage("La URL no puede exceder 2000 caracteres.")
            .Must(BeValidAbsoluteUri).WithMessage("La URL no es válida.");

        RuleFor(x => x.ResourceType)
            .IsInEnum().WithMessage("Tipo de recurso inválido.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("El orden debe ser mayor o igual a cero.");
    }

    private static bool BeValidAbsoluteUri(string url) => Uri.TryCreate(url, UriKind.Absolute, out _);
}
