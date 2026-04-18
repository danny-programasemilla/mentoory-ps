using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.ReorderSubjectTemplates;

/// <summary>
/// Validator for the <see cref="ReorderSubjectTemplatesCommand"/>.
/// </summary>
public class ReorderSubjectTemplatesValidator : AbstractValidator<ReorderSubjectTemplatesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderSubjectTemplatesValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public ReorderSubjectTemplatesValidator()
    {
        RuleFor(x => x.TemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla es requerido.");

        RuleFor(x => x.TopicExternalId)
            .NotEmpty().WithMessage("El identificador del tema es requerido.");

        RuleFor(x => x.SubjectExternalIdsInOrder)
            .NotEmpty().WithMessage("La lista de materias no puede estar vacía.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("La lista de materias no puede contener identificadores duplicados.");
    }
}
