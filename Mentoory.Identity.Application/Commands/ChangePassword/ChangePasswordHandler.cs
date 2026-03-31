using Mentoory.Identity.Domain.Repositories;
using Mentoory.Identity.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Identity.Application.Commands.ChangePassword;

/// <summary>
/// Handles password change by verifying the current password and enforcing password history.
/// </summary>
public partial class ChangePasswordHandler : BaseCommandHandler<ChangePasswordCommand>
{
    private const int PasswordHistoryDepth = 5;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<ChangePasswordHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <param name="passwordHasher">The password hasher for verifying and securing credentials.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
    public ChangePasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ILogger<ChangePasswordHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
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

        // Check password history
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        if (user.HasUsedPassword(newPasswordHash, PasswordHistoryDepth))
        {
            return Failure(ResultErrorCodes.GenericError, ("ChangePassword", "La nueva contraseña no puede ser igual a las últimas 5 contraseñas."));
        }

        user.ChangePassword(newPasswordHash, utcNow);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogPasswordChanged(user.Email.Value);

        return Success();
    }

    /// <summary>
    /// Logs when a user's password is changed successfully.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Password changed for user: {Email}")]
    partial void LogPasswordChanged(string email);
}
