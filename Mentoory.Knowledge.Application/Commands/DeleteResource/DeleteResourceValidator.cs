using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.DeleteResource;

/// <summary>
/// Validator for the <see cref="DeleteResourceCommand"/>.
/// </summary>
public class DeleteResourceValidator : AbstractValidator<DeleteResourceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteResourceValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public DeleteResourceValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.ResourceExternalId)
            .NotEmpty().WithMessage("El identificador del recurso es requerido.");
    }
}
