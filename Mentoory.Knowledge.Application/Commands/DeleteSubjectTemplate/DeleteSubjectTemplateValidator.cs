using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.DeleteSubjectTemplate;

/// <summary>
/// Validator for the <see cref="DeleteSubjectTemplateCommand"/>.
/// </summary>
public class DeleteSubjectTemplateValidator : AbstractValidator<DeleteSubjectTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSubjectTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public DeleteSubjectTemplateValidator()
    {
        RuleFor(x => x.TemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla es requerido.");

        RuleFor(x => x.SubjectExternalId)
            .NotEmpty().WithMessage("El identificador de la materia es requerido.");
    }
}
