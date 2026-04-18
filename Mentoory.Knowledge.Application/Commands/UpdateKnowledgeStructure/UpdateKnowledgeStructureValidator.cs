using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.UpdateKnowledgeStructure;

/// <summary>
/// Validator for the <see cref="UpdateKnowledgeStructureCommand"/>.
/// </summary>
public class UpdateKnowledgeStructureValidator : AbstractValidator<UpdateKnowledgeStructureCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateKnowledgeStructureValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public UpdateKnowledgeStructureValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción no puede exceder 2000 caracteres.")
            .When(x => x.Description is not null);
    }
}
