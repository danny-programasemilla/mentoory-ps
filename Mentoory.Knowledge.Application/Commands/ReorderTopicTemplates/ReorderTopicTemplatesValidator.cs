using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.ReorderTopicTemplates;

/// <summary>
/// Validator for the <see cref="ReorderTopicTemplatesCommand"/>.
/// </summary>
public class ReorderTopicTemplatesValidator : AbstractValidator<ReorderTopicTemplatesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderTopicTemplatesValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public ReorderTopicTemplatesValidator()
    {
        RuleFor(x => x.TemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla es requerido.");

        RuleFor(x => x.ModuleExternalId)
            .NotEmpty().WithMessage("El identificador del módulo es requerido.");

        RuleFor(x => x.TopicExternalIdsInOrder)
            .NotEmpty().WithMessage("La lista de temas es requerida.")
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage("No se permiten temas duplicados en la lista.");
    }
}
