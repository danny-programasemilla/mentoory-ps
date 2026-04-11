using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.AssignMentor;

/// <summary>
/// Validator for the AssignMentorCommand.
/// </summary>
public class AssignMentorValidator : AbstractValidator<AssignMentorCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssignMentorValidator"/> class.
    /// </summary>
    public AssignMentorValidator()
    {
        RuleFor(x => x.ProjectExternalId)
            .NotEmpty().WithMessage("ProjectExternalId is required");

        RuleFor(x => x.MentorUserId)
            .GreaterThan(0).WithMessage("MentorUserId must be greater than 0");

        RuleFor(x => x.EntrepreneurUserId)
            .GreaterThan(0).WithMessage("EntrepreneurUserId must be greater than 0");
    }
}
