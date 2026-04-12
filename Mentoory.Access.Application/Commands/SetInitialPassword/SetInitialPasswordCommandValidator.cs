using FluentValidation;

namespace Mentoory.Access.Application.Commands.SetInitialPassword;

public class SetInitialPasswordCommandValidator : AbstractValidator<SetInitialPasswordCommand>
{
    public SetInitialPasswordCommandValidator()
    {
        RuleFor(x => x.UserExternalId)
            .NotEmpty().WithMessage("El usuario es requerido.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("El token es requerido.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(12).WithMessage("La contraseña debe tener al menos 12 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un dígito.")
            .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe contener al menos un carácter especial.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Las contraseñas no coinciden.");
    }
}
