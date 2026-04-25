using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.AdvanceProjectStage;

public sealed class AdvanceProjectStageValidator : AbstractValidator<AdvanceProjectStageCommand>
{
    public AdvanceProjectStageValidator()
    {
        RuleFor(c => c.ProjectExternalId)
            .NotEmpty()
            .WithMessage("ProjectExternalId is required");

        RuleFor(c => c.ActingUserId)
            .GreaterThan(0)
            .WithMessage("ActingUserId must be greater than 0");
    }
}
