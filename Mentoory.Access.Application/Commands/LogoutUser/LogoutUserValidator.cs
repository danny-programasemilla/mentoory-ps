using FluentValidation;

namespace Mentoory.Access.Application.Commands.LogoutUser;

/// <summary>
/// Validator for the <see cref="LogoutUserCommand"/>.
/// </summary>
public class LogoutUserValidator : AbstractValidator<LogoutUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutUserValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public LogoutUserValidator()
    {
        RuleFor(x => x.SessionToken)
            .NotEmpty().WithMessage("El token de sesión es requerido.");
    }
}
