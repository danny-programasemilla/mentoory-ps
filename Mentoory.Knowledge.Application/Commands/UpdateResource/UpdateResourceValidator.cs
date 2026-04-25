using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.UpdateResource;

/// <summary>
/// Validator for the <see cref="UpdateResourceCommand"/>.
/// </summary>
public class UpdateResourceValidator : AbstractValidator<UpdateResourceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateResourceValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public UpdateResourceValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.ResourceExternalId)
            .NotEmpty().WithMessage("El identificador del recurso es requerido.");

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
    }

    private static bool BeValidAbsoluteUri(string url) => Uri.TryCreate(url, UriKind.Absolute, out _);
}
