using FluentValidation;

namespace Mentoory.Access.Application.Commands.RegisterInternalUser;

public class RegisterInternalUserValidator : AbstractValidator<RegisterInternalUserCommand>
{
    public RegisterInternalUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El correo electrónico no es válido.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("El país es requerido.");

        RuleFor(x => x.Identification)
            .NotEmpty().WithMessage("El número de identificación es requerido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(12).WithMessage("La contraseña debe tener al menos 12 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un dígito.")
            .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe contener al menos un carácter especial.");

        RuleFor(x => x.ProjectExternalId)
            .NotEmpty().WithMessage("El proyecto es requerido.");
    }
}
