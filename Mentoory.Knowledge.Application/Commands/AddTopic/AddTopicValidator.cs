using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.AddTopic;

/// <summary>
/// Validator for the <see cref="AddTopicCommand"/>.
/// </summary>
public class AddTopicValidator : AbstractValidator<AddTopicCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddTopicValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public AddTopicValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.ModuleExternalId)
            .NotEmpty().WithMessage("El identificador del módulo es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción no puede exceder 2000 caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("El orden debe ser mayor o igual a cero.");
    }
}
