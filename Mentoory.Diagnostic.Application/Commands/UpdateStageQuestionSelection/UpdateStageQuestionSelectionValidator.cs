using FluentValidation;

namespace Mentoory.Diagnostic.Application.Commands.UpdateStageQuestionSelection;

public class UpdateStageQuestionSelectionValidator : AbstractValidator<UpdateStageQuestionSelectionCommand>
{
    public UpdateStageQuestionSelectionValidator()
    {
        RuleFor(x => x.AssignmentExternalId)
            .NotEmpty().WithMessage("AssignmentExternalId is required");

        RuleFor(x => x.SelectedQuestionIds)
            .NotEmpty().WithMessage("At least one question must be selected");
    }
}
