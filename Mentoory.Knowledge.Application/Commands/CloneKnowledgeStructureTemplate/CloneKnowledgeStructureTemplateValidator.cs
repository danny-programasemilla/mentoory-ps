using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.CloneKnowledgeStructureTemplate;

/// <summary>
/// Validator for the <see cref="CloneKnowledgeStructureTemplateCommand"/>.
/// </summary>
public class CloneKnowledgeStructureTemplateValidator : AbstractValidator<CloneKnowledgeStructureTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloneKnowledgeStructureTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public CloneKnowledgeStructureTemplateValidator()
    {
        RuleFor(x => x.SourceTemplateExternalId)
            .NotEmpty().WithMessage("El identificador de la plantilla de origen es requerido.");

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("El identificador del proyecto es requerido.");

        RuleFor(x => x.IncubatorId)
            .GreaterThan(0).WithMessage("El identificador de la incubadora es requerido.");
    }
}
