using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.DeleteTopic;

/// <summary>
/// Validator for the <see cref="DeleteTopicCommand"/>.
/// </summary>
public class DeleteTopicValidator : AbstractValidator<DeleteTopicCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTopicValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public DeleteTopicValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.TopicExternalId)
            .NotEmpty().WithMessage("El identificador del tema es requerido.");
    }
}
