using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.ReorderProjectStages;

public class ReorderProjectStagesValidator : AbstractValidator<ReorderProjectStagesCommand>
{
    public ReorderProjectStagesValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("ProjectId is required");

        RuleFor(x => x.OrderedStageExternalIds)
            .NotEmpty().WithMessage("Ordered stage IDs are required");
    }
}
