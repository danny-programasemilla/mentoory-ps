using FluentValidation;

namespace Mentoory.Access.Application.Commands.AdminVerifyEmail;

public class AdminVerifyEmailValidator : AbstractValidator<AdminVerifyEmailCommand>
{
    public AdminVerifyEmailValidator()
    {
        RuleFor(x => x.UserExternalId)
            .NotEmpty().WithMessage("El identificador de usuario es requerido.");
    }
}
