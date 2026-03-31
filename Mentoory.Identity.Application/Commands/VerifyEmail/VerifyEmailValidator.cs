using FluentValidation;

namespace Mentoory.Identity.Application.Commands.VerifyEmail;

/// <summary>
/// Validator for the <see cref="VerifyEmailCommand"/>.
/// </summary>
public class VerifyEmailValidator : AbstractValidator<VerifyEmailCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyEmailValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public VerifyEmailValidator()
    {
        RuleFor(x => x.TokenHash)
            .NotEmpty().WithMessage("El token de verificación es requerido.");
    }
}
