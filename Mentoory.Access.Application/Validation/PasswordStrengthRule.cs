using FluentValidation;

namespace Mentoory.Access.Application.Validation;

public static class PasswordStrengthRule
{
    public const int MinimumLength = 12;

    public static IRuleBuilderOptions<T, string> MustBeStrongPassword<T>(this IRuleBuilder<T, string> builder)
    {
        return builder
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(MinimumLength).WithMessage($"La contraseña debe tener al menos {MinimumLength} caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un dígito.")
            .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe contener al menos un carácter especial.");
    }
}
