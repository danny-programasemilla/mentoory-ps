using FluentValidation;

namespace Mentoory.Access.Application.Users.Commands.AdminResetPassword;

/// <summary>
/// Validator for the <see cref="AdminResetPasswordCommand"/>.
/// </summary>
public class AdminResetPasswordValidator : AbstractValidator<AdminResetPasswordCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminResetPasswordValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public AdminResetPasswordValidator()
    {
        RuleFor(x => x.UserExternalId)
            .NotEmpty().WithMessage("El identificador de usuario es inválido.");
    }
}
