using Mentoory.Identity.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Identity.Application.Commands.LogoutUser;

/// <summary>
/// Handles user logout by deactivating the specified session.
/// </summary>
public partial class LogoutUserHandler : BaseCommandHandler<LogoutUserCommand>
{
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ILogger<LogoutUserHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutUserHandler"/> class.
    /// </summary>
    /// <param name="authSessionRepository">The auth session repository for session management.</param>
    /// <param name="logger">The logger instance.</param>
    public LogoutUserHandler(
        IAuthSessionRepository authSessionRepository,
        ILogger<LogoutUserHandler> logger)
    {
        _authSessionRepository = authSessionRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
    {
        var session = await _authSessionRepository.GetByTokenAsync(request.SessionToken, cancellationToken);
        if (session is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Logout", "Sesión no encontrada."));
        }

        session.Deactivate();
        _authSessionRepository.Update(session);
        await _authSessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogUserLoggedOut(session.UserId);

        return Success();
    }

    /// <summary>
    /// Logs when a user logs out successfully.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "User logged out. UserId: {UserId}")]
    partial void LogUserLoggedOut(long userId);
}
