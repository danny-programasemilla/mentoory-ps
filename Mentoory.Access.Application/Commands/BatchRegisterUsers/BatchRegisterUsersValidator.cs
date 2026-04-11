using FluentValidation;

namespace Mentoory.Access.Application.Commands.BatchRegisterUsers;

public class BatchRegisterUsersValidator : AbstractValidator<BatchRegisterUsersCommand>
{
    public BatchRegisterUsersValidator()
    {
        RuleFor(x => x.Rows)
            .NotEmpty().WithMessage("La lista de usuarios no puede estar vacía.");

        RuleFor(x => x.Rows.Count)
            .LessThanOrEqualTo(500).WithMessage("El máximo de filas permitido es 500.")
            .When(x => x.Rows is not null);

        RuleFor(x => x.ProjectExternalId)
            .NotEmpty().WithMessage("El proyecto es requerido.");

        RuleForEach(x => x.Rows).ChildRules(row =>
        {
            row.RuleFor(r => r.Country)
                .NotEmpty().WithMessage("El país es requerido.");

            row.RuleFor(r => r.Identification)
                .NotEmpty().WithMessage("El número de identificación es requerido.");

            row.RuleFor(r => r.Email)
                .NotEmpty().WithMessage("El correo electrónico es requerido.")
                .EmailAddress().WithMessage("El correo electrónico no es válido.");

            row.RuleFor(r => r.FirstName)
                .NotEmpty().WithMessage("El nombre es requerido.")
                .MaximumLength(100);

            row.RuleFor(r => r.LastName)
                .NotEmpty().WithMessage("El apellido es requerido.")
                .MaximumLength(100);
        });
    }
}
