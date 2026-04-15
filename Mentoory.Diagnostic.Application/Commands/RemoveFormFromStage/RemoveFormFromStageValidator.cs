using FluentValidation;

namespace Mentoory.Diagnostic.Application.Commands.RemoveFormFromStage;

public class RemoveFormFromStageValidator : AbstractValidator<RemoveFormFromStageCommand>
{
    public RemoveFormFromStageValidator()
    {
        RuleFor(x => x.AssignmentExternalId)
            .NotEmpty().WithMessage("AssignmentExternalId is required");
    }
}
