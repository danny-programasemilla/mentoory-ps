using Mentoory.Identity.Domain.Repositories;
using Mentoory.Identity.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Identity.Application.Commands.ResetPassword;

/// <summary>
/// Handles password reset by validating the reset token and updating the user's password.
/// </summary>
public partial class ResetPasswordHandler : BaseCommandHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<ResetPasswordHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <param name="passwordHasher">The password hasher for securing the new credential.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
    public ResetPasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ILogger<ResetPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByPasswordResetTokenAsync(request.TokenHash, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("ResetPassword", "Token de restablecimiento inválido."));
        }

        var token = user.PasswordResetTokens.FirstOrDefault(t => t.TokenHash == request.TokenHash);
        if (token is null || !token.IsValid(utcNow))
        {
            return Failure(ResultErrorCodes.GenericError, ("ResetPassword", "Token de restablecimiento inválido o expirado."));
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);

        token.MarkAsUsed();
        user.ChangePassword(newPasswordHash, utcNow);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogPasswordReset(user.Email.Value);

        return Success();
    }

    /// <summary>
    /// Logs when a user's password is reset successfully.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Password reset for user: {Email}")]
    partial void LogPasswordReset(string email);
}
