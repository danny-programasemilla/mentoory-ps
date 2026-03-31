using FluentValidation;

namespace Mentoory.Authorization.Application.Commands.RevokeRole;

/// <summary>
/// Validator for the RevokeRoleCommand that ensures the command data is valid.
/// </summary>
public class RevokeRoleValidator : AbstractValidator<RevokeRoleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevokeRoleValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public RevokeRoleValidator()
    {
        RuleFor(x => x.RoleAssignmentExternalId)
            .NotEqual(Guid.Empty)
            .WithMessage("RoleAssignmentExternalId is required.");
    }
}
