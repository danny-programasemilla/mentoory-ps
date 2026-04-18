using FluentValidation;
using Mentoory.Access.Application.Validation;

namespace Mentoory.Access.Application.Commands.AdminEnrollUser;

public class AdminEnrollUserValidator : AbstractValidator<AdminEnrollUserCommand>
{
    public AdminEnrollUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El correo electrónico no es válido.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("El país es requerido.");

        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("El número de identificación es requerido.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.Password)
            .MustBeStrongPassword()
            .MustNotContainIdentifyingData(x => x.Email, x => x.NationalId);
    }
}
