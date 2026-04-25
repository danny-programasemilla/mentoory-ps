using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.ArchiveKnowledgeStructureTemplate;

/// <summary>
/// Validator for the <see cref="ArchiveKnowledgeStructureTemplateCommand"/>.
/// </summary>
public class ArchiveKnowledgeStructureTemplateValidator : AbstractValidator<ArchiveKnowledgeStructureTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveKnowledgeStructureTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public ArchiveKnowledgeStructureTemplateValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty().WithMessage("El identificador es requerido.");
    }
}
