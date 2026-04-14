using System.Security.Cryptography;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Contracts.IntegrationEvents;
using Mentoory.Access.Domain.Aggregates.AuthSession;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.LoginUser;

public partial class LoginUserHandler : BaseCommandHandler<LoginUserCommand, LoginUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ISystemConfigurationReader _configReader;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<LoginUserHandler> _logger;

    public LoginUserHandler(
        IUserRepository userRepository,
        IAuthSessionRepository authSessionRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ISystemConfigurationReader configReader,
        IIntegrationEventService eventService,
        ILogger<LoginUserHandler> logger)
    {
        _userRepository = userRepository;
        _authSessionRepository = authSessionRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _configReader = configReader;
        _eventService = eventService;
        _logger = logger;
    }

    public override async Task<Result<LoginUserResult>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Login", "Credenciales inválidas."));
        }

        // Read config-driven values
        var maxAttempts = await _configReader.GetIntAsync(nameof(ConfigurationKey.MaxFailedLoginAttempts), cancellationToken);
        var lockoutMinutes = await _configReader.GetIntAsync(nameof(ConfigurationKey.LockoutDurationMinutes), cancellationToken);
        var sessionTimeoutHours = await _configReader.GetIntAsync(nameof(ConfigurationKey.SessionTimeoutHours), cancellationToken);

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
            user.RecordFailedLogin(utcNow, maxAttempts, TimeSpan.FromMinutes(lockoutMinutes));
            _userRepository.Update(user);
            await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            LogLoginFailed(normalizedEmail);
            return Failure(ResultErrorCodes.GenericError, ("Login", "Credenciales inválidas."));
        }

        var failedAttemptCount = user.FailedLoginAttempts;
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
            TimeSpan.FromHours(sessionTimeoutHours));

        _authSessionRepository.Add(session);
        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogLoginSucceeded(normalizedEmail);

        await _eventService.PublishAsync(
            new LoginAttemptEvent(
                user.Id,
                user.Email.Value,
                true,
                request.IpAddress,
                request.UserAgent,
                failedAttemptCount,
                utcNow),
            cancellationToken);

        return Success(new LoginUserResult(
            session,
            user.AccountStatus == AccountStatus.PasswordResetRequired));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Login failed for email: {Email}")]
    partial void LogLoginFailed(string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Login succeeded for email: {Email}")]
    partial void LogLoginSucceeded(string email);
}
