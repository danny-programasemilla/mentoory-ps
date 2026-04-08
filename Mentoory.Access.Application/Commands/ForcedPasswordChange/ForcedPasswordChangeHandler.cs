using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.ForcedPasswordChange;

public partial class ForcedPasswordChangeHandler : BaseCommandHandler<ForcedPasswordChangeCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ISystemConfigurationReader _configReader;
    private readonly ILogger<ForcedPasswordChangeHandler> _logger;

    public ForcedPasswordChangeHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ISystemConfigurationReader configReader,
        ILogger<ForcedPasswordChangeHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _configReader = configReader;
        _logger = logger;
    }

    public override async Task<Result> Handle(ForcedPasswordChangeCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("ChangePassword", "Usuario no encontrado."));
        }

        // Verify current password
        var activeCredential = user.GetActiveCredential();
        if (activeCredential is null || !_passwordHasher.VerifyPassword(request.CurrentPassword, activeCredential.PasswordHash))
        {
            return Failure(ResultErrorCodes.GenericError, ("CurrentPassword", "La contraseña actual es incorrecta."));
        }

        // Check password history
        var historyDepth = await _configReader.GetIntAsync(
            nameof(ConfigurationKey.PasswordHistoryDepth), cancellationToken);
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        if (user.HasUsedPassword(newPasswordHash, historyDepth))
        {
            return Failure(ResultErrorCodes.GenericError, ("NewPassword", $"La nueva contraseña no puede ser igual a las últimas {historyDepth} contraseñas."));
        }

        user.ChangePassword(newPasswordHash, utcNow);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogForcedPasswordChanged(user.Email.Value);

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Forced password change completed for user: {Email}")]
    partial void LogForcedPasswordChanged(string email);
}
