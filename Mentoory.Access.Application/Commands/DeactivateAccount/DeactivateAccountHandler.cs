using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.DeactivateAccount;

/// <summary>
/// Handles deactivating a user account.
/// </summary>
public partial class DeactivateAccountHandler : BaseCommandHandler<DeactivateAccountCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<DeactivateAccountHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeactivateAccountHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
    public DeactivateAccountHandler(
        IUserRepository userRepository,
        ITimeProvider timeProvider,
        ILogger<DeactivateAccountHandler> logger)
    {
        _userRepository = userRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("DeactivateAccount", "Usuario no encontrado."));
        }

        user.Deactivate(utcNow);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogAccountDeactivated(user.Email.Value);

        return Success();
    }

    /// <summary>
    /// Logs when a user account is deactivated.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Account deactivated for user: {Email}")]
    partial void LogAccountDeactivated(string email);
}
