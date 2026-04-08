using FluentValidation;

namespace Mentoory.Access.Application.Configuration.Commands.UpdateConfiguration;

/// <summary>
/// Validator for the <see cref="UpdateConfigurationCommand"/>.
/// </summary>
public class UpdateConfigurationValidator : AbstractValidator<UpdateConfigurationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateConfigurationValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public UpdateConfigurationValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("La clave de configuración es obligatoria.");

        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("El valor de configuración es obligatorio.");
    }
}
