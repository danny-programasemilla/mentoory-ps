using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.DeleteKnowledgeStructureTemplate;

/// <summary>
/// Validator for the <see cref="DeleteKnowledgeStructureTemplateCommand"/>.
/// </summary>
public class DeleteKnowledgeStructureTemplateValidator : AbstractValidator<DeleteKnowledgeStructureTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteKnowledgeStructureTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public DeleteKnowledgeStructureTemplateValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty().WithMessage("El identificador es requerido.");
    }
}
