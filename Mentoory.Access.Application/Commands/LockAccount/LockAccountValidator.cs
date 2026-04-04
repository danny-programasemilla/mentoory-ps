using FluentValidation;

namespace Mentoory.Access.Application.Commands.LockAccount;

/// <summary>
/// Validator for the <see cref="LockAccountCommand"/>.
/// </summary>
public class LockAccountValidator : AbstractValidator<LockAccountCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LockAccountValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public LockAccountValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("El identificador de usuario es inválido.");

        RuleFor(x => x.Duration)
            .GreaterThan(TimeSpan.Zero).WithMessage("La duración de bloqueo debe ser mayor a cero.");
    }
}
