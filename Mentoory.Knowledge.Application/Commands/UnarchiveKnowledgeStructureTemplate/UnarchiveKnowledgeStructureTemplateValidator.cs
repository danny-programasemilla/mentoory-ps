using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.UnarchiveKnowledgeStructureTemplate;

/// <summary>
/// Validator for the <see cref="UnarchiveKnowledgeStructureTemplateCommand"/>.
/// </summary>
public class UnarchiveKnowledgeStructureTemplateValidator : AbstractValidator<UnarchiveKnowledgeStructureTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnarchiveKnowledgeStructureTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public UnarchiveKnowledgeStructureTemplateValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty().WithMessage("El identificador es requerido.");
    }
}
