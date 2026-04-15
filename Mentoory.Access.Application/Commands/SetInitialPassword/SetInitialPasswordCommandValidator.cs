using FluentValidation;

namespace Mentoory.Access.Application.Commands.SetInitialPassword;

/// <summary>
/// Validator for the <see cref="SetInitialPasswordCommand"/>.
/// </summary>
public class SetInitialPasswordCommandValidator : AbstractValidator<SetInitialPasswordCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetInitialPasswordCommandValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public SetInitialPasswordCommandValidator()
    {
        RuleFor(x => x.UserExternalId)
            .NotEmpty().WithMessage("El identificador del usuario es requerido.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("El token de verificación es requerido.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es requerida.")
            .MinimumLength(12).WithMessage("La contraseña debe tener al menos 12 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un dígito.")
            .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe contener al menos un carácter especial.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("La confirmación de contraseña no coincide con la nueva contraseña.");
    }
}
