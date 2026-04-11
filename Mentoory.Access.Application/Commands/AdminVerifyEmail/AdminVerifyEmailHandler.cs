using Mentoory.Access.Application.IntegrationEvents;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.AdminVerifyEmail;

public partial class AdminVerifyEmailHandler : BaseCommandHandler<AdminVerifyEmailCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<AdminVerifyEmailHandler> _logger;

    public AdminVerifyEmailHandler(
        IUserRepository userRepository,
        ITimeProvider timeProvider,
        IIntegrationEventService eventService,
        ILogger<AdminVerifyEmailHandler> logger)
    {
        _userRepository = userRepository;
        _timeProvider = timeProvider;
        _eventService = eventService;
        _logger = logger;
    }

    public override async Task<Result> Handle(AdminVerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByExternalIdAsync(request.UserExternalId, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("AdminVerify", "Usuario no encontrado."));
        }

        user.AdminVerifyEmail(utcNow);
        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogAdminVerified(user.Email.Value);

        await _eventService.PublishAsync(
            new UserEmailVerifiedEvent(user.Id, user.ExternalId, user.Email.Value, utcNow),
            cancellationToken);

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Admin verified email for user: {Email}")]
    partial void LogAdminVerified(string email);
}
