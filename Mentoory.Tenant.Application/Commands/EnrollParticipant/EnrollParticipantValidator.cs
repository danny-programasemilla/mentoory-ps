using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.EnrollParticipant;

/// <summary>
/// Validator for the EnrollParticipantCommand.
/// </summary>
public class EnrollParticipantValidator : AbstractValidator<EnrollParticipantCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnrollParticipantValidator"/> class.
    /// </summary>
    public EnrollParticipantValidator()
    {
        RuleFor(x => x.ProjectExternalId)
            .NotEmpty().WithMessage("ProjectExternalId is required");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be greater than 0");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .MaximumLength(50).WithMessage("Role must not exceed 50 characters");
    }
}
