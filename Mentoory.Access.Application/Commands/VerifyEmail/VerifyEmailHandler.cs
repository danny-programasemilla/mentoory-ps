using Mentoory.Access.Application.IntegrationEvents;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.VerifyEmail;

/// <summary>
/// Handles email verification by validating the token and activating the user account.
/// </summary>
public partial class VerifyEmailHandler : BaseCommandHandler<VerifyEmailCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<VerifyEmailHandler> _logger;

    public VerifyEmailHandler(
        IUserRepository userRepository,
        ITimeProvider timeProvider,
        IIntegrationEventService eventService,
        ILogger<VerifyEmailHandler> logger)
    {
        _userRepository = userRepository;
        _timeProvider = timeProvider;
        _eventService = eventService;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByEmailVerificationTokenAsync(request.TokenHash, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Verification", "Token de verificación inválido."));
        }

        var token = user.EmailVerificationTokens.FirstOrDefault(t => t.TokenHash == request.TokenHash);
        if (token is null || !token.IsValid(utcNow))
        {
            return Failure(ResultErrorCodes.GenericError, ("Verification", "Token de verificación inválido o expirado."));
        }

        token.MarkAsUsed();
        user.VerifyEmail(utcNow);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogEmailVerified(user.Email.Value);

        await _eventService.PublishAsync(
            new UserEmailVerifiedEvent(user.Id, user.ExternalId, user.Email.Value, utcNow),
            cancellationToken);

        return Success();
    }

    /// <summary>
    /// Logs when a user's email is successfully verified.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Email verified for user: {Email}")]
    partial void LogEmailVerified(string email);
}
