using FluentValidation;

namespace Mentoory.Diagnostic.Application.Commands.SetFormTemplateKnowledgeBinding;

/// <summary>
/// Validator for <see cref="SetFormTemplateKnowledgeBindingCommand"/>.
/// </summary>
public class SetFormTemplateKnowledgeBindingValidator
    : AbstractValidator<SetFormTemplateKnowledgeBindingCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetFormTemplateKnowledgeBindingValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public SetFormTemplateKnowledgeBindingValidator()
    {
        RuleFor(x => x.FormTemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla de formulario es requerido.");
    }
}
