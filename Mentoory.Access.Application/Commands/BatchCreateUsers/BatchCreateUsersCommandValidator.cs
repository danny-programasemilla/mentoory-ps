using FluentValidation;

namespace Mentoory.Access.Application.Commands.BatchCreateUsers;

public class BatchCreateUsersCommandValidator : AbstractValidator<BatchCreateUsersCommand>
{
    public BatchCreateUsersCommandValidator()
    {
        RuleFor(x => x.Rows)
            .NotEmpty().WithMessage("El archivo no contiene filas.")
            .Must(rows => rows.Count <= 500).WithMessage("El archivo no puede contener más de 500 filas.");

        RuleFor(x => x.ProjectExternalId)
            .NotEmpty().WithMessage("El proyecto es requerido.");
    }
}
