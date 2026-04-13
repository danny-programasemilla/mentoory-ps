using Mentoory.Access.Application.IntegrationEvents;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.SetInitialPassword;

public partial class SetInitialPasswordCommandHandler
    : BaseCommandHandler<SetInitialPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<SetInitialPasswordCommandHandler> _logger;

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

    public override async Task<Result> Handle(
        SetInitialPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByExternalIdAsync(request.UserExternalId, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("User", "Usuario no encontrado."));
        }

        var token = user.EmailVerificationTokens.FirstOrDefault(
            t => _passwordHasher.VerifyPassword(request.Token, t.TokenHash));

        if (token is null || !token.IsValid(utcNow))
        {
            return Failure(ResultErrorCodes.GenericError, ("Token", "Token de verificación inválido o expirado."));
        }

        token.MarkAsUsed();

        var passwordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.ChangePassword(passwordHash, utcNow);

        if (user.AccountStatus == AccountStatus.PendingVerification)
        {
            user.VerifyEmail(utcNow);
        }

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        await _eventService.PublishAsync(
            new UserEmailVerifiedEvent(user.Id, user.ExternalId, user.Email.Value, utcNow),
            cancellationToken);

        LogPasswordSet(user.Email.Value);
        return Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Password set via verification for user: {Email}")]
    partial void LogPasswordSet(string email);
}
