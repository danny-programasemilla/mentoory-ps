using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.AddProjectStage;

public class AddProjectStageValidator : AbstractValidator<AddProjectStageCommand>
{
    public AddProjectStageValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("ProjectId is required");

        RuleFor(x => x.Position)
            .GreaterThan(0).WithMessage("Position must be greater than zero");
    }
}
