using Mentoory.Identity.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Identity.Application.Commands.UnlockAccount;

/// <summary>
/// Handles unlocking a user account.
/// </summary>
public partial class UnlockAccountHandler : BaseCommandHandler<UnlockAccountCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<UnlockAccountHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnlockAccountHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
    public UnlockAccountHandler(
        IUserRepository userRepository,
        ITimeProvider timeProvider,
        ILogger<UnlockAccountHandler> logger)
    {
        _userRepository = userRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(UnlockAccountCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("UnlockAccount", "Usuario no encontrado."));
        }

        user.Unlock(utcNow);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogAccountUnlocked(user.Email.Value);

        return Success();
    }

    /// <summary>
    /// Logs when a user account is unlocked.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Account unlocked for user: {Email}")]
    partial void LogAccountUnlocked(string email);
}
