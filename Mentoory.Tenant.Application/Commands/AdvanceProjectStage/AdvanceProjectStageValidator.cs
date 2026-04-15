using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.AdvanceProjectStage;

public class AdvanceProjectStageValidator : AbstractValidator<AdvanceProjectStageCommand>
{
    public AdvanceProjectStageValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("ProjectId is required");

        RuleFor(x => x.AdvancedByUserId)
            .GreaterThan(0).WithMessage("AdvancedByUserId must be a positive value");
    }
}
