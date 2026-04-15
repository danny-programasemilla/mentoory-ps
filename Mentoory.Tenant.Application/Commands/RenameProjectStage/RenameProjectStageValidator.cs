using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.RenameProjectStage;

public class RenameProjectStageValidator : AbstractValidator<RenameProjectStageCommand>
{
    public RenameProjectStageValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("ProjectId is required");

        RuleFor(x => x.StageExternalId)
            .NotEmpty().WithMessage("StageExternalId is required");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("DisplayName is required")
            .MaximumLength(200).WithMessage("DisplayName must not exceed 200 characters");
    }
}
