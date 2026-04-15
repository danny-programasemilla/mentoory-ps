using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.RemoveProjectStage;

public class RemoveProjectStageValidator : AbstractValidator<RemoveProjectStageCommand>
{
    public RemoveProjectStageValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("ProjectId is required");

        RuleFor(x => x.StageExternalId)
            .NotEmpty().WithMessage("StageExternalId is required");
    }
}
