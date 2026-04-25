using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.DeleteSubject;

/// <summary>
/// Validator for the <see cref="DeleteSubjectCommand"/>.
/// </summary>
public class DeleteSubjectValidator : AbstractValidator<DeleteSubjectCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSubjectValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public DeleteSubjectValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.SubjectExternalId)
            .NotEmpty().WithMessage("El identificador de la materia es requerido.");
    }
}
