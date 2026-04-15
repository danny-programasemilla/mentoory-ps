using System.Security.Cryptography;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Contracts.IntegrationEvents;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Services;

public partial class UserRegistrationService : IUserRegistrationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ISystemConfigurationReader _configReader;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<UserRegistrationService> _logger;

    public UserRegistrationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ISystemConfigurationReader configReader,
        IIntegrationEventService eventService,
        ILogger<UserRegistrationService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _configReader = configReader;
        _eventService = eventService;
        _logger = logger;
    }

    public async Task<Result<UserRegistrationResult>> RegisterAsync(
        UserRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var utcNow = _timeProvider.UtcNow;

        if (await _userRepository.ExistsByNationalIdentityAsync(request.Country, request.NationalId, cancellationToken))
        {
            return Result<UserRegistrationResult>.Failure(
                ResultErrorCodes.GenericError,
                ("NationalId", "Ya existe una cuenta con este número de identificación."));
        }

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return Result<UserRegistrationResult>.Failure(
                ResultErrorCodes.GenericError,
                ("Email", "Ya existe una cuenta con este correo electrónico."));
        }

        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        var user = User.Register(
            request.Email,
            request.Country,
            request.NationalId,
            request.FirstName,
            request.LastName,
            hashedPassword,
            utcNow);

        if (request.EmailVerificationMode == EmailVerificationMode.Required)
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

        if (request.RequirePasswordReset)
        {
            user.SetPasswordResetRequired(utcNow);
        }

        _userRepository.Add(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        var invitationExpiryHours = await _configReader.GetIntAsync(
            nameof(ConfigurationKey.InvitationTokenExpiryHours), cancellationToken);

        await _eventService.PublishAsync(
            new UserRegisteredEvent(
                user.Id,
                user.ExternalId,
                user.Email.Value,
                user.FirstName,
                user.LastName,
                user.AccountStatus.ToString(),
                request.ProjectExternalId,
                request.EmailVerificationMode == EmailVerificationMode.Required,
                request.EnrollmentVariant,
                invitationExpiryHours,
                user.CreatedAtUtc,
                _timeProvider.UtcNow),
            cancellationToken);

        LogUserRegistered(user.Email.Value, request.EnrollmentVariant);

        return Result.Success(new UserRegistrationResult(
            user.Id,
            user.ExternalId,
            user.Email.Value,
            user.AccountStatus.ToString(),
            user.CreatedAtUtc));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User registered successfully. Email: {Email}, Variant: {Variant}")]
    partial void LogUserRegistered(string email, string variant);
}
