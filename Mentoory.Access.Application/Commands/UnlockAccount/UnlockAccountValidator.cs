using FluentValidation;

namespace Mentoory.Access.Application.Commands.UnlockAccount;

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
        RuleFor(x => x.UserExternalId)
            .NotEmpty().WithMessage("El identificador de usuario es inválido.");
    }
}
