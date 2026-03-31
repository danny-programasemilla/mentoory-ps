using FluentValidation;

namespace Mentoory.Identity.Application.Commands.UnlockAccount;

/// <summary>
/// Validator for the <see cref="UnlockAccountCommand"/>.
/// </summary>
public class UnlockAccountValidator : AbstractValidator<UnlockAccountCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnlockAccountValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public UnlockAccountValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("El identificador de usuario es inválido.");
    }
}
