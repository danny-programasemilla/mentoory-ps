using Mentoory.Identity.Application.IntegrationEvents;
using Mentoory.Identity.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Identity.Application.Commands.LockAccount;

/// <summary>
/// Handles locking a user account.
/// </summary>
public partial class LockAccountHandler : BaseCommandHandler<LockAccountCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<LockAccountHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LockAccountHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="eventService">The integration event service for publishing events.</param>
    /// <param name="logger">The logger instance.</param>
    public LockAccountHandler(
        IUserRepository userRepository,
        ITimeProvider timeProvider,
        IIntegrationEventService eventService,
        ILogger<LockAccountHandler> logger)
    {
        _userRepository = userRepository;
        _timeProvider = timeProvider;
        _eventService = eventService;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(LockAccountCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("LockAccount", "Usuario no encontrado."));
        }

        user.Lock(utcNow, request.Duration);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogAccountLocked(user.Email.Value);

        await _eventService.PublishAsync(
            new UserLockedOutEvent(user.Id, user.ExternalId, user.Email.Value, utcNow),
            cancellationToken);

        return Success();
    }

    /// <summary>
    /// Logs when a user account is locked.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Account locked for user: {Email}")]
    partial void LogAccountLocked(string email);
}
