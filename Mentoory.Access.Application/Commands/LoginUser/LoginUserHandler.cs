using System.Security.Cryptography;
using Mentoory.Access.Domain.Aggregates.AuthSession;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.LoginUser;

/// <summary>
/// Handles user login by validating credentials and creating an authentication session.
/// </summary>
public partial class LoginUserHandler : BaseCommandHandler<LoginUserCommand, AuthSession>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<LoginUserHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginUserHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <param name="authSessionRepository">The auth session repository for session management.</param>
    /// <param name="passwordHasher">The password hasher for verifying credentials.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
    public LoginUserHandler(
        IUserRepository userRepository,
        IAuthSessionRepository authSessionRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ILogger<LoginUserHandler> logger)
    {
        _userRepository = userRepository;
        _authSessionRepository = authSessionRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result<AuthSession>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Login", "Credenciales inválidas."));
        }

        // Check lockout
        if (user.IsLockedOut(utcNow))
        {
            return Failure(ResultErrorCodes.GenericError, ("Login", "Cuenta bloqueada temporalmente."));
        }

        // Check account status
        if (user.AccountStatus == AccountStatus.PendingVerification)
        {
            return Failure(ResultErrorCodes.GenericError, ("Login", "Verifique su correo electrónico."));
        }

        if (user.AccountStatus == AccountStatus.Disabled)
        {
            return Failure(ResultErrorCodes.GenericError, ("Login", "Cuenta deshabilitada."));
        }

        // Validate password
        var activeCredential = user.GetActiveCredential();
        if (activeCredential is null || !_passwordHasher.VerifyPassword(request.Password, activeCredential.PasswordHash))
        {
            user.RecordFailedLogin(utcNow, maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
            _userRepository.Update(user);
            await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            LogLoginFailed(normalizedEmail);
            return Failure(ResultErrorCodes.GenericError, ("Login", "Credenciales inválidas."));
        }

        user.RecordSuccessfulLogin(utcNow);

        // Invalidate existing active sessions (single-session enforcement)
        var activeSessions = await _authSessionRepository.GetActiveSessionsByUserIdAsync(user.Id, cancellationToken);
        foreach (var existingSession in activeSessions)
        {
            existingSession.Deactivate();
            _authSessionRepository.Update(existingSession);
        }

        // Generate session token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var sessionToken = Convert.ToBase64String(tokenBytes);

        var session = AuthSession.Create(
            sessionToken,
            user.Id,
            request.IpAddress,
            request.UserAgent,
            utcNow,
            TimeSpan.FromHours(8));

        _authSessionRepository.Add(session);
        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogLoginSucceeded(normalizedEmail);

        return Success(session);
    }

    /// <summary>
    /// Logs when a login attempt fails.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Login failed for email: {Email}")]
    partial void LogLoginFailed(string email);

    /// <summary>
    /// Logs when a login attempt succeeds.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Login succeeded for email: {Email}")]
    partial void LogLoginSucceeded(string email);
}
