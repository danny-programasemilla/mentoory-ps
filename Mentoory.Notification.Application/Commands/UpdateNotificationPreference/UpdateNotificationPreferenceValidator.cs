using FluentValidation;
using Mentoory.Notification.Domain.Enums;

namespace Mentoory.Notification.Application.Commands.UpdateNotificationPreference;

public class UpdateNotificationPreferenceValidator : AbstractValidator<UpdateNotificationPreferenceCommand>
{
    public UpdateNotificationPreferenceValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("El identificador de usuario es inválido.");

        RuleFor(x => x.NotificationType)
            .IsInEnum().WithMessage("El tipo de notificación es inválido.");

        RuleFor(x => x)
            .Must(x => x.IsEnabled || x.NotificationType == NotificationType.LoginAlert)
            .WithMessage("No se puede deshabilitar este tipo de notificación. Las notificaciones de registro e invitación son obligatorias.");
    }
}
