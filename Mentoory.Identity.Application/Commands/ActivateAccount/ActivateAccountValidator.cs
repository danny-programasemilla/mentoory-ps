using FluentValidation;

namespace Mentoory.Identity.Application.Commands.ActivateAccount;

/// <summary>
/// Validator for the <see cref="ActivateAccountCommand"/>.
/// </summary>
public class ActivateAccountValidator : AbstractValidator<ActivateAccountCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivateAccountValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public ActivateAccountValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("El identificador de usuario es inválido.");
    }
}
