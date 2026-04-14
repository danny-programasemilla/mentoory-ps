using System.Security.Cryptography;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Contracts.IntegrationEvents;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.RegisterInternalUser;

public partial class RegisterInternalUserHandler
    : BaseCommandHandler<RegisterInternalUserCommand, RegisterInternalUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ISystemConfigurationReader _configReader;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<RegisterInternalUserHandler> _logger;

    public RegisterInternalUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ISystemConfigurationReader configReader,
        IIntegrationEventService eventService,
        ILogger<RegisterInternalUserHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _configReader = configReader;
        _eventService = eventService;
        _logger = logger;
    }

    public override async Task<Result<RegisterInternalUserResult>> Handle(
        RegisterInternalUserCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        // Field-specific uniqueness checks
        if (await _userRepository.ExistsByNationalIdentityAsync(request.Country, request.Identification, cancellationToken))
        {
            return Failure(ResultErrorCodes.GenericError, ("Identification", "Ya existe una cuenta con este número de identificación."));
        }

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return Failure(ResultErrorCodes.GenericError, ("Email", "Ya existe una cuenta con este correo electrónico."));
        }

        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        var user = User.Register(
            request.Email,
            request.Country,
            request.Identification,
            string.Empty, // FirstName — internal registration may not have it
            string.Empty, // LastName — internal registration may not have it
            hashedPassword,
            utcNow);

        if (request.RequireEmailVerification)
        {
            var expiryHours = await _configReader.GetIntAsync(
                nameof(ConfigurationKey.EmailVerificationTokenExpiryHours), cancellationToken);
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var tokenHash = _passwordHasher.HashPassword(Convert.ToBase64String(tokenBytes));
            user.GenerateEmailVerificationToken(utcNow, tokenHash, expiryHours);
        }
        else
        {
            user.AdminVerifyEmail(utcNow);
        }

        _userRepository.Add(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Read invitation expiry for the event
        var invitationExpiryHours = await _configReader.GetIntAsync(
            nameof(ConfigurationKey.InvitationTokenExpiryHours), cancellationToken);

        // Publish integration event for cross-domain enrollment
        await _eventService.PublishAsync(
            new UserRegisteredEvent(
                user.Id,
                user.ExternalId,
                user.Email.Value,
                user.FirstName,
                user.LastName,
                user.AccountStatus.ToString(),
                request.ProjectExternalId,
                request.RequireEmailVerification,
                "FullFlow", // Internal registration always uses project's variant, determined by handler
                invitationExpiryHours,
                user.CreatedAtUtc,
                utcNow),
            cancellationToken);

        LogInternalUserRegistered(user.Email.Value);

        var enrollmentStatus = request.RequireEmailVerification
            ? "PendingVerification"
            : "Enrolled";

        return Success(new RegisterInternalUserResult(user.ExternalId, enrollmentStatus));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Internal user registered: {Email}")]
    partial void LogInternalUserRegistered(string email);
}
