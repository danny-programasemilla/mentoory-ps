using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.SetLeadMentor;

/// <summary>
/// Validator for the SetLeadMentorCommand.
/// </summary>
public class SetLeadMentorValidator : AbstractValidator<SetLeadMentorCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetLeadMentorValidator"/> class.
    /// </summary>
    public SetLeadMentorValidator()
    {
        RuleFor(x => x.ProjectExternalId)
            .NotEmpty().WithMessage("ProjectExternalId is required");

        RuleFor(x => x.MentorAssignmentExternalId)
            .NotEmpty().WithMessage("MentorAssignmentExternalId is required");
    }
}
