using FluentValidation;

namespace Mentoory.Access.Application.Commands.RegenerateVerificationToken;

public class RegenerateVerificationTokenValidator : AbstractValidator<RegenerateVerificationTokenCommand>
{
    public RegenerateVerificationTokenValidator()
    {
        RuleFor(x => x.UserExternalId)
            .NotEmpty().WithMessage("El identificador de usuario es requerido.");
    }
}
