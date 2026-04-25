using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.UpdateSubject;

/// <summary>
/// Validator for the <see cref="UpdateSubjectCommand"/>.
/// </summary>
public class UpdateSubjectValidator : AbstractValidator<UpdateSubjectCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSubjectValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public UpdateSubjectValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.SubjectExternalId)
            .NotEmpty().WithMessage("El identificador de la materia es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción no puede exceder 2000 caracteres.")
            .When(x => x.Description is not null);
    }
}
