using FluentValidation;

namespace Mentoory.Identity.Application.Commands.LoginUser;

/// <summary>
/// Validator for the <see cref="LoginUserCommand"/>.
/// </summary>
public class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginUserValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public LoginUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.");

        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("IP address is required.");
    }
}
