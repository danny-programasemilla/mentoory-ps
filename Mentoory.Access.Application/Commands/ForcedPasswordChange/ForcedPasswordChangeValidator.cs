using FluentValidation;

namespace Mentoory.Access.Application.Commands.ForcedPasswordChange;

public class ForcedPasswordChangeValidator : AbstractValidator<ForcedPasswordChangeCommand>
{
    public ForcedPasswordChangeValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("El identificador de usuario es inválido.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("La contraseña actual es requerida.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es requerida.")
            .MinimumLength(12).WithMessage("La contraseña debe tener al menos 12 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un dígito.")
            .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe contener al menos un carácter especial.");
    }
}
