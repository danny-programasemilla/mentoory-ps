using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.ChangePassword;

public partial class ChangePasswordHandler : BaseCommandHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ISystemConfigurationReader _configReader;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ISystemConfigurationReader configReader,
        ILogger<ChangePasswordHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _configReader = configReader;
        _logger = logger;
    }

    public override async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
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
            return Failure(ResultErrorCodes.GenericError, ("ChangePassword", "La contraseña actual es incorrecta."));
        }

        // Check password history from config
        var historyDepth = await _configReader.GetIntAsync(
            nameof(ConfigurationKey.PasswordHistoryDepth), cancellationToken);
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        if (user.HasUsedPassword(newPasswordHash, historyDepth))
        {
            return Failure(ResultErrorCodes.GenericError, ("ChangePassword", $"La nueva contraseña no puede ser igual a las últimas {historyDepth} contraseñas."));
        }

        user.ChangePassword(newPasswordHash, utcNow);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogPasswordChanged(user.Email.Value);

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Password changed for user: {Email}")]
    partial void LogPasswordChanged(string email);
}
