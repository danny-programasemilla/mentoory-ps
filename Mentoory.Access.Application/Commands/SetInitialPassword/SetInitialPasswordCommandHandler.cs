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

        return request.TokenType switch
        {
            TokenType.Verification => await HandleVerificationTokenAsync(user, request, utcNow, cancellationToken),
            TokenType.Invitation => await HandleInvitationTokenAsync(user, request, utcNow, cancellationToken),
            _ => Failure(ResultErrorCodes.GenericError, ("TokenType", "Tipo de token inválido.")),
        };
    }

    private async Task<Result> HandleVerificationTokenAsync(
        Access.Domain.Aggregates.User.User user,
        SetInitialPasswordCommand request,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var token = user.EmailVerificationTokens.FirstOrDefault(
            t => t.TokenHash == request.Token);

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

        LogPasswordSetViaVerification(user.Email.Value);
        return Success();
    }

    private async Task<Result> HandleInvitationTokenAsync(
        Access.Domain.Aggregates.User.User user,
        SetInitialPasswordCommand request,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        // For invitation tokens, we only set the password
        // The invitation acceptance is handled separately by the Tenant domain
        var hasActiveCredential = user.GetActiveCredential() is not null;

        if (!hasActiveCredential || user.AccountStatus == AccountStatus.PasswordResetRequired)
        {
            var passwordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.ChangePassword(passwordHash, utcNow);

            _userRepository.Update(user);
            await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        LogPasswordSetViaInvitation(user.Email.Value);
        return Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Password set via verification for user: {Email}")]
    partial void LogPasswordSetViaVerification(string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Password set via invitation for user: {Email}")]
    partial void LogPasswordSetViaInvitation(string email);
}
