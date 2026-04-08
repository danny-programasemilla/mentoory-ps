using System.Security.Cryptography;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Users.Commands.AdminResetPassword;

/// <summary>
/// Handles the AdminResetPasswordCommand by generating a temporary password,
/// updating the user's credential, and setting password-reset-required status.
/// </summary>
public partial class AdminResetPasswordHandler
    : BaseCommandHandler<AdminResetPasswordCommand, AdminResetPasswordResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<AdminResetPasswordHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminResetPasswordHandler"/> class.
    /// </summary>
    public AdminResetPasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ILogger<AdminResetPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result<AdminResetPasswordResult>> Handle(
        AdminResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByExternalIdAsync(request.UserExternalId, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("AdminResetPassword", "Usuario no encontrado."));
        }

        var tempPassword = GenerateTemporaryPassword();
        var hashedPassword = _passwordHasher.HashPassword(tempPassword);

        user.ChangePassword(hashedPassword, utcNow);
        user.SetPasswordResetRequired(utcNow);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogAdminPasswordReset(user.Email.Value);

        return Success(new AdminResetPasswordResult(tempPassword));
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        var chars = new char[16];
        var rng = RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);

        chars[0] = upper[bytes[0] % upper.Length];
        chars[1] = lower[bytes[1] % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = special[bytes[3] % special.Length];

        var all = upper + lower + digits + special;
        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = all[bytes[i] % all.Length];
        }

        // Fisher-Yates shuffle
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = bytes[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    /// <summary>
    /// Logs when an admin resets a user's password.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Admin reset password for user: {Email}")]
    partial void LogAdminPasswordReset(string email);
}
