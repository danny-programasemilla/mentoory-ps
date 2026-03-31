using FluentValidation;

namespace Mentoory.Authorization.Application.Commands.SetActiveContext;

/// <summary>
/// Validator for the SetActiveContextCommand that ensures the command data is valid.
/// </summary>
public class SetActiveContextValidator : AbstractValidator<SetActiveContextCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetActiveContextValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public SetActiveContextValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId must be greater than 0.");

        RuleFor(x => x.RoleAssignmentExternalId)
            .NotEqual(Guid.Empty)
            .WithMessage("RoleAssignmentExternalId is required.");
    }
}
