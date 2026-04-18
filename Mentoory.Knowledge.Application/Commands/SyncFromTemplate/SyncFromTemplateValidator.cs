using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.SyncFromTemplate;

/// <summary>
/// Validator for the <see cref="SyncFromTemplateCommand"/>.
/// </summary>
public class SyncFromTemplateValidator : AbstractValidator<SyncFromTemplateCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncFromTemplateValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public SyncFromTemplateValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");
    }
}
