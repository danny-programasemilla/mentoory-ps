using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.CreateKnowledgeStructureTemplate;

/// <summary>
/// Validator for the <see cref="CreateKnowledgeStructureTemplateCommand"/>.
/// </summary>
public class CreateKnowledgeStructureTemplateValidator : AbstractValidator<CreateKnowledgeStructureTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateKnowledgeStructureTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public CreateKnowledgeStructureTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("La descripción no puede exceder 2000 caracteres.")
            .When(x => x.Description is not null);
    }
}
