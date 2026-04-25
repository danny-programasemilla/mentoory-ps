using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.SetSyncMode;

/// <summary>
/// Validator for the <see cref="SetSyncModeCommand"/>.
/// </summary>
public class SetSyncModeValidator : AbstractValidator<SetSyncModeCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetSyncModeValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public SetSyncModeValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.SyncMode)
            .IsInEnum().WithMessage("Modo de sincronización inválido.");
    }
}
