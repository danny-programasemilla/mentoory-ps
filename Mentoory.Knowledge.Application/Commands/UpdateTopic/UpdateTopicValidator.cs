using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.UpdateTopic;

/// <summary>
/// Validator for the <see cref="UpdateTopicCommand"/>.
/// </summary>
public class UpdateTopicValidator : AbstractValidator<UpdateTopicCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTopicValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public UpdateTopicValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.TopicExternalId)
            .NotEmpty().WithMessage("El identificador del tema es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción no puede exceder 2000 caracteres.")
            .When(x => x.Description is not null);
    }
}
