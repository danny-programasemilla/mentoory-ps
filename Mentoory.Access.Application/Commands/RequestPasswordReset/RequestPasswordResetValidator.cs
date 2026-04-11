using FluentValidation;

namespace Mentoory.Access.Application.Commands.RequestPasswordReset;

/// <summary>
/// Validator for the <see cref="RequestPasswordResetCommand"/>.
/// </summary>
public class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestPasswordResetValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public RequestPasswordResetValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El correo electrónico no es válido.");
    }
}
