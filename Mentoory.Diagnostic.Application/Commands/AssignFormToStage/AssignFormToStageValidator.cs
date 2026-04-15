using FluentValidation;

namespace Mentoory.Diagnostic.Application.Commands.AssignFormToStage;

public class AssignFormToStageValidator : AbstractValidator<AssignFormToStageCommand>
{
    public AssignFormToStageValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("ProjectId is required");

        RuleFor(x => x.IncubatorId)
            .GreaterThan(0).WithMessage("IncubatorId is required");

        RuleFor(x => x.ProjectStageId)
            .GreaterThan(0).WithMessage("ProjectStageId is required");

        RuleFor(x => x.ProjectFormExternalId)
            .NotEmpty().WithMessage("ProjectFormExternalId is required");
    }
}
