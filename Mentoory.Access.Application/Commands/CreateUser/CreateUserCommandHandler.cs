using System.Security.Cryptography;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Application.Services;
using Mentoory.Access.Contracts.IntegrationEvents;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
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
    private readonly IUserRegistrationService _registrationService;
    private readonly ISystemConfigurationReader _configReader;
    private readonly IIntegrationEventService _eventService;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUserRegistrationService registrationService,
        ISystemConfigurationReader configReader,
        IIntegrationEventService eventService,
        ITimeProvider timeProvider,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _registrationService = registrationService;
        _configReader = configReader;
        _eventService = eventService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override async Task<Result<CreateUserResult>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var warnings = new List<string>();

        var existingByIdentity = await _userRepository.GetByNationalIdentityAsync(
            request.Country, request.Identification, cancellationToken);

        if (existingByIdentity is not null)
        {
            return await HandleExistingUserAsync(existingByIdentity, request, normalizedEmail, warnings, cancellationToken);
        }

        var existingByEmail = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingByEmail is not null)
        {
            return await HandleExistingUserAsync(existingByEmail, request, normalizedEmail, warnings, cancellationToken);
        }

        return await CreateNewUserAsync(request, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        var bothTogglesOn = request.SkipEmailVerification && request.SkipInvitationAcceptance;
        string? tempPassword = null;

        string password;
        if (bothTogglesOn)
        {
            tempPassword = GenerateTemporaryPassword();
            password = tempPassword;
        }
        else
        {
            password = Guid.NewGuid().ToString("N");
        }

        var verificationMode = request.SkipEmailVerification
            ? EmailVerificationMode.Skipped
            : EmailVerificationMode.Required;

        var enrollmentVariant = request.SkipInvitationAcceptance
            ? EnrollmentVariants.Bypass
            : EnrollmentVariants.FullFlow;

        var registrationRequest = new UserRegistrationRequest(
            request.Email,
            request.Country,
            request.Identification,
            request.FirstName,
            request.LastName,
            password,
            request.ProjectExternalId,
            verificationMode,
            enrollmentVariant,
            RequirePasswordReset: bothTogglesOn);

        var result = await _registrationService.RegisterAsync(registrationRequest, cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.ErrorCode!.Value, result.ErrorMessages!);
        }

        LogUserCreated(result.Value!.Email, request.ProjectExternalId);

        return Success(new CreateUserResult(
            result.Value.UserExternalId,
            CreateUserOutcome.Created,
            tempPassword,
            []));
    }

    private async Task<Result<CreateUserResult>> HandleExistingUserAsync(
        Domain.Aggregates.User.User existingUser,
        CreateUserCommand request,
        string normalizedEmail,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        if (existingUser.AccountStatus is AccountStatus.Locked or AccountStatus.Disabled)
        {
            var statusName = existingUser.AccountStatus == AccountStatus.Locked ? "Bloqueada" : "Deshabilitada";
            warnings.Add($"Usuario inscrito pero la cuenta está {statusName}.");
        }

        var invitationExpiryHours = await _configReader.GetIntAsync(
            nameof(ConfigurationKey.InvitationTokenExpiryHours), cancellationToken);

        var requiresVerification = existingUser.AccountStatus == AccountStatus.PendingVerification
                                   && !request.SkipEmailVerification;
        var enrollmentVariant = request.SkipInvitationAcceptance
            ? EnrollmentVariants.Bypass
            : EnrollmentVariants.FullFlow;

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
