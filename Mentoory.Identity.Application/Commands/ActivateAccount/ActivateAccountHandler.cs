using Mentoory.Identity.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Identity.Application.Commands.ActivateAccount;

/// <summary>
/// Handles activating a user account.
/// </summary>
public partial class ActivateAccountHandler : BaseCommandHandler<ActivateAccountCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<ActivateAccountHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivateAccountHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
    public ActivateAccountHandler(
        IUserRepository userRepository,
        ITimeProvider timeProvider,
        ILogger<ActivateAccountHandler> logger)
    {
        _userRepository = userRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(ActivateAccountCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("ActivateAccount", "Usuario no encontrado."));
        }

        user.Activate(utcNow);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogAccountActivated(user.Email.Value);

        return Success();
    }

    /// <summary>
    /// Logs when a user account is activated.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Account activated for user: {Email}")]
    partial void LogAccountActivated(string email);
}
