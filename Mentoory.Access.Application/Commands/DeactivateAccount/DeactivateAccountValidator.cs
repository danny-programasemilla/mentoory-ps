using FluentValidation;

namespace Mentoory.Access.Application.Commands.DeactivateAccount;

/// <summary>
/// Validator for the <see cref="DeactivateAccountCommand"/>.
/// </summary>
public class DeactivateAccountValidator : AbstractValidator<DeactivateAccountCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeactivateAccountValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public DeactivateAccountValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("El identificador de usuario es inválido.");
    }
}
