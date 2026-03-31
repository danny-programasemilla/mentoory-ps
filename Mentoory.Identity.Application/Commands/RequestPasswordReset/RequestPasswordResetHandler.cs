using Mentoory.Identity.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Identity.Application.Commands.RequestPasswordReset;

/// <summary>
/// Handles a password reset request. Always returns success to prevent user enumeration.
/// </summary>
public partial class RequestPasswordResetHandler : BaseCommandHandler<RequestPasswordResetCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<RequestPasswordResetHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestPasswordResetHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
    public RequestPasswordResetHandler(
        IUserRepository userRepository,
        ITimeProvider timeProvider,
        ILogger<RequestPasswordResetHandler> logger)
    {
        _userRepository = userRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            // Always return success to prevent user enumeration
            LogPasswordResetRequestedForUnknownEmail(normalizedEmail);
            return Success();
        }

        // Generate a token hash (the actual token delivery is handled via integration events / notifications)
        var tokenHash = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        user.GeneratePasswordResetToken(utcNow, tokenHash);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogPasswordResetRequested(normalizedEmail);

        // The notification module will handle sending the reset email via integration event
        return Success();
    }

    /// <summary>
    /// Logs when a password reset is requested.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Password reset requested for email: {Email}")]
    partial void LogPasswordResetRequested(string email);

    /// <summary>
    /// Logs when a password reset is requested for an unknown email.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Password reset requested for unknown email: {Email}")]
    partial void LogPasswordResetRequestedForUnknownEmail(string email);
}
