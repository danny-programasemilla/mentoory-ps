using Mentoory.Access.Contracts.IntegrationEvents;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.SetInitialPassword;

/// <summary>
/// Handles setting the initial password for a user during email verification.
/// Validates the verification token, sets the password, and activates the account.
/// </summary>
public partial class SetInitialPasswordCommandHandler : BaseCommandHandler<SetInitialPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<SetInitialPasswordCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetInitialPasswordCommandHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <param name="passwordHasher">The password hasher for securing the new credential.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="eventService">The integration event service for publishing events.</param>
    /// <param name="logger">The logger instance.</param>
    public SetInitialPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        IIntegrationEventService eventService,
        ILogger<SetInitialPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _eventService = eventService;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(SetInitialPasswordCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByExternalIdAsync(request.UserExternalId, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("UserExternalId", "Usuario no encontrado."));
        }

        var matchingToken = FindValidToken(user, request.Token, utcNow);
        if (matchingToken is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Token", "Token de verificación inválido o expirado."));
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.ChangePassword(newPasswordHash, utcNow);
        matchingToken.MarkAsUsed();

        if (user.AccountStatus == AccountStatus.PendingVerification)
        {
            user.VerifyEmail(utcNow);
        }

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogInitialPasswordSet(user.Email.Value);

        await _eventService.PublishAsync(
            new UserEmailVerifiedEvent(user.Id, user.ExternalId, user.Email.Value, utcNow),
            cancellationToken);

        return Success();
    }

    private Domain.Aggregates.User.EmailVerificationToken? FindValidToken(
        Domain.Aggregates.User.User user,
        string rawToken,
        DateTime utcNow)
    {
        foreach (var token in user.EmailVerificationTokens)
        {
            if (!token.IsUsed && token.IsValid(utcNow) && _passwordHasher.VerifyPassword(rawToken, token.TokenHash))
            {
                return token;
            }
        }

        return null;
    }

    /// <summary>
    /// Logs when a user's initial password is set successfully.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Initial password set for user: {Email}")]
    partial void LogInitialPasswordSet(string email);
}
