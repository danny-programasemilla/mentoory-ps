using FluentValidation;

namespace Mentoory.Access.Application.Commands.AssignRole;

/// <summary>
/// Validator for the AssignRoleCommand that ensures the command data is valid.
/// </summary>
public class AssignRoleValidator : AbstractValidator<AssignRoleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssignRoleValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public AssignRoleValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId must be greater than 0.");

        RuleFor(x => x.IncubatorId)
            .GreaterThan(0)
            .WithMessage("IncubatorId must be greater than 0.");

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Role is required.");
    }
}
