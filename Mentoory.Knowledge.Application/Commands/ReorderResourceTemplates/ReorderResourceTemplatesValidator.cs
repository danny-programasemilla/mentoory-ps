using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.ReorderResourceTemplates;

/// <summary>
/// Validator for the <see cref="ReorderResourceTemplatesCommand"/>.
/// </summary>
public class ReorderResourceTemplatesValidator : AbstractValidator<ReorderResourceTemplatesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderResourceTemplatesValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public ReorderResourceTemplatesValidator()
    {
        RuleFor(x => x.TemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla es requerido.");

        RuleFor(x => x.SubjectExternalId)
            .NotEmpty().WithMessage("El identificador de la materia es requerido.");

        RuleFor(x => x.ResourceExternalIdsInOrder)
            .NotEmpty().WithMessage("La lista de recursos no puede estar vacía.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("La lista de recursos no puede contener identificadores duplicados.");
    }
}
