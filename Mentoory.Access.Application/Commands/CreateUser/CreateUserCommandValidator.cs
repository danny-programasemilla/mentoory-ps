using FluentValidation;

namespace Mentoory.Access.Application.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El correo electrónico no es válido.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("El país es requerido.");

        RuleFor(x => x.Identification)
            .NotEmpty().WithMessage("El número de identificación es requerido.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido.");

        RuleFor(x => x.ProjectExternalId)
            .NotEmpty().WithMessage("El proyecto es requerido.");
    }
}
