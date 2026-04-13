using System.Security.Cryptography;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Application.IntegrationEvents;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.CreateUser;

public partial class CreateUserCommandHandler
    : BaseCommandHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ISystemConfigurationReader _configReader;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ISystemConfigurationReader configReader,
        IIntegrationEventService eventService,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _configReader = configReader;
        _eventService = eventService;
        _logger = logger;
    }

    public override async Task<Result<CreateUserResult>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var warnings = new List<string>();

        // Check if user already exists by country+identification
        var existingByIdentity = await _userRepository.GetByNationalIdentityAsync(
            request.Country, request.Identification, cancellationToken);

        if (existingByIdentity is not null)
        {
            return await HandleExistingUserAsync(existingByIdentity, request, normalizedEmail, warnings, utcNow, cancellationToken);
        }

        // Check by email
        var existingByEmail = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingByEmail is not null)
        {
            return await HandleExistingUserAsync(existingByEmail, request, normalizedEmail, warnings, utcNow, cancellationToken);
        }

        return await CreateNewUserAsync(request, utcNow, cancellationToken);
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        var chars = new char[16];
        var bytes = RandomNumberGenerator.GetBytes(16);

        chars[0] = upper[bytes[0] % upper.Length];
        chars[1] = lower[bytes[1] % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = special[bytes[3] % special.Length];

        var all = upper + lower + digits + special;
        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = all[bytes[i] % all.Length];
        }

        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = bytes[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private async Task<Result<CreateUserResult>> CreateNewUserAsync(
        CreateUserCommand request,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var bothTogglesOn = request.SkipEmailVerification && request.SkipInvitationAcceptance;
        string? tempPassword = null;

        string passwordHash;
        if (bothTogglesOn)
        {
            tempPassword = GenerateTemporaryPassword();
            passwordHash = _passwordHasher.HashPassword(tempPassword);
        }
        else
        {
            // No password — user will set it during onboarding
            passwordHash = _passwordHasher.HashPassword(Guid.NewGuid().ToString("N"));
        }

        var user = User.Register(
            request.Email,
            request.Country,
            request.Identification,
            request.FirstName,
            request.LastName,
            passwordHash,
            utcNow);

        if (request.SkipEmailVerification)
        {
            user.AdminVerifyEmail(utcNow);
        }
        else
        {
            var expiryHours = await _configReader.GetIntAsync(
                nameof(ConfigurationKey.EmailVerificationTokenExpiryHours), cancellationToken);
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var tokenHash = _passwordHasher.HashPassword(Convert.ToBase64String(tokenBytes));
            user.GenerateEmailVerificationToken(utcNow, tokenHash, expiryHours);
        }

        if (bothTogglesOn)
        {
            user.SetPasswordResetRequired(utcNow);
        }

        _userRepository.Add(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Publish integration event for cross-domain enrollment
        var invitationExpiryHours = await _configReader.GetIntAsync(
            nameof(ConfigurationKey.InvitationTokenExpiryHours), cancellationToken);

        var requiresVerification = !request.SkipEmailVerification;
        var enrollmentVariant = request.SkipInvitationAcceptance ? EnrollmentVariants.Bypass : EnrollmentVariants.FullFlow;

        await _eventService.PublishAsync(
            new UserRegisteredEvent(
                user.Id,
                user.ExternalId,
                user.Email.Value,
                user.FirstName,
                user.LastName,
                user.AccountStatus.ToString(),
                request.ProjectExternalId,
                requiresVerification,
                enrollmentVariant,
                invitationExpiryHours,
                user.CreatedAtUtc,
                utcNow),
            cancellationToken);

        LogUserCreated(user.Email.Value, request.ProjectExternalId);

        return Success(new CreateUserResult(
            user.ExternalId,
            CreateUserOutcome.Created,
            tempPassword,
            []));
    }

    private async Task<Result<CreateUserResult>> HandleExistingUserAsync(
        User existingUser,
        CreateUserCommand request,
        string normalizedEmail,
        List<string> warnings,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        // Add warnings for status issues
        if (existingUser.AccountStatus is AccountStatus.Locked or AccountStatus.Disabled)
        {
            var statusName = existingUser.AccountStatus == AccountStatus.Locked ? "Bloqueada" : "Deshabilitada";
            warnings.Add($"Usuario inscrito pero la cuenta está {statusName}.");
        }

        var invitationExpiryHours = await _configReader.GetIntAsync(
            nameof(ConfigurationKey.InvitationTokenExpiryHours), cancellationToken);

        // For existing users, email verification state is inherited
        var requiresVerification = existingUser.AccountStatus == AccountStatus.PendingVerification
                                   && !request.SkipEmailVerification;
        var enrollmentVariant = request.SkipInvitationAcceptance ? EnrollmentVariants.Bypass : EnrollmentVariants.FullFlow;

        await _eventService.PublishAsync(
            new UserRegisteredEvent(
                existingUser.Id,
                existingUser.ExternalId,
                existingUser.Email.Value,
                existingUser.FirstName,
                existingUser.LastName,
                existingUser.AccountStatus.ToString(),
                request.ProjectExternalId,
                requiresVerification,
                enrollmentVariant,
                invitationExpiryHours,
                existingUser.CreatedAtUtc,
                utcNow),
            cancellationToken);

        LogExistingUserEnrolled(existingUser.Email.Value, request.ProjectExternalId);

        return Success(new CreateUserResult(
            existingUser.ExternalId,
            CreateUserOutcome.Enrolled,
            null,
            warnings));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User created: {Email} for project {ProjectId}")]
    partial void LogUserCreated(string email, Guid projectId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Existing user enrolled: {Email} for project {ProjectId}")]
    partial void LogExistingUserEnrolled(string email, Guid projectId);
}
