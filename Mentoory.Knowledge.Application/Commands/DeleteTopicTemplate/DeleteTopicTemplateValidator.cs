using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.DeleteTopicTemplate;

/// <summary>
/// Validator for the <see cref="DeleteTopicTemplateCommand"/>.
/// </summary>
public class DeleteTopicTemplateValidator : AbstractValidator<DeleteTopicTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTopicTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public DeleteTopicTemplateValidator()
    {
        RuleFor(x => x.TemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla es requerido.");

        RuleFor(x => x.TopicExternalId)
            .NotEmpty().WithMessage("El identificador del tema es requerido.");
    }
}
