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

namespace Mentoory.Access.Application.Commands.BatchRegisterUsers;

public partial class BatchRegisterUsersHandler
    : BaseCommandHandler<BatchRegisterUsersCommand, BatchRegistrationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ISystemConfigurationReader _configReader;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<BatchRegisterUsersHandler> _logger;

    public BatchRegisterUsersHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ISystemConfigurationReader configReader,
        IIntegrationEventService eventService,
        ILogger<BatchRegisterUsersHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _configReader = configReader;
        _eventService = eventService;
        _logger = logger;
    }

    public override async Task<Result<BatchRegistrationResult>> Handle(
        BatchRegisterUsersCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;
        var rows = new List<BatchRowResult>();
        var successCount = 0;
        var skippedCount = 0;
        var errorCount = 0;

        var invitationExpiryHours = await _configReader.GetIntAsync(
            nameof(ConfigurationKey.InvitationTokenExpiryHours), cancellationToken);

        var records = request.Rows;

        for (var i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var rowNumber = i + 1;

            try
            {
                var rowResult = await ProcessRowAsync(
                    record, rowNumber, request.ProjectExternalId, invitationExpiryHours, utcNow, cancellationToken);

                rows.Add(rowResult);

                switch (rowResult.Status)
                {
                    case "Success": successCount++; break;
                    case "Skipped": skippedCount++; break;
                    case "Error": errorCount++; break;
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                rows.Add(new BatchRowResult
                {
                    RowNumber = rowNumber,
                    Country = record.Country,
                    Identification = record.Identification,
                    Email = record.Email,
                    Status = "Error",
                    ErrorMessage = ex.Message,
                });
                LogRowError(rowNumber, ex.Message);
            }
        }

        var result = new BatchRegistrationResult(records.Count, successCount, skippedCount, errorCount, rows);
        LogBatchCompleted(records.Count, successCount, skippedCount, errorCount);

        return Success(result);
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        var chars = new char[16];
        var rng = RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);

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

    private async Task<BatchRowResult> ProcessRowAsync(
        BatchUserRow record,
        int rowNumber,
        Guid projectExternalId,
        int invitationExpiryHours,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        var normalizedEmail = record.Email.Trim().ToUpperInvariant();

        // Check if user exists by country+identification
        var existingUser = await _userRepository.GetByNationalIdentityAsync(
            record.Country, record.Identification, cancellationToken);

        if (existingUser is not null)
        {
            // User exists — check if email matches
            if (existingUser.Email.NormalizedValue != normalizedEmail)
            {
                warnings.Add($"El correo del CSV ({record.Email}) difiere del registrado ({existingUser.Email.Value}).");
            }

            // Already exists — publish event for enrollment check
            await _eventService.PublishAsync(
                new UserRegisteredEvent(
                    existingUser.Id,
                    existingUser.ExternalId,
                    existingUser.Email.Value,
                    existingUser.FirstName,
                    existingUser.LastName,
                    existingUser.AccountStatus.ToString(),
                    projectExternalId,
                    false,
                    EnrollmentVariants.FullFlow,
                    invitationExpiryHours,
                    existingUser.CreatedAtUtc,
                    utcNow),
                cancellationToken);

            return new BatchRowResult
            {
                RowNumber = rowNumber,
                Country = record.Country,
                Identification = record.Identification,
                Email = record.Email,
                UserAlreadyExisted = true,
                Status = "Skipped",
                Warnings = warnings,
            };
        }

        // Check email uniqueness
        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return new BatchRowResult
            {
                RowNumber = rowNumber,
                Country = record.Country,
                Identification = record.Identification,
                Email = record.Email,
                Status = "Error",
                ErrorMessage = "Ya existe una cuenta con este correo electrónico.",
            };
        }

        // Generate temporary password
        var tempPassword = GenerateTemporaryPassword();
        var hashedPassword = _passwordHasher.HashPassword(tempPassword);

        var user = User.Register(
            record.Email,
            record.Country,
            record.Identification,
            record.FirstName,
            record.LastName,
            hashedPassword,
            utcNow);

        // Set PasswordResetRequired so user must change on first login
        user.AdminVerifyEmail(utcNow); // Verify immediately (batch users skip email verification)
        user.SetPasswordResetRequired(utcNow);

        _userRepository.Add(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Publish event for enrollment
        await _eventService.PublishAsync(
            new UserRegisteredEvent(
                user.Id, user.ExternalId, user.Email.Value,
                user.FirstName, user.LastName,
                user.AccountStatus.ToString(),
                projectExternalId, false, EnrollmentVariants.FullFlow,
                invitationExpiryHours, user.CreatedAtUtc, utcNow),
            cancellationToken);

        return new BatchRowResult
        {
            RowNumber = rowNumber,
            Country = record.Country,
            Identification = record.Identification,
            Email = record.Email,
            UserCreated = true,
            TemporaryPassword = tempPassword,
            Status = "Success",
            Warnings = warnings,
        };
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Batch row {RowNumber} error: {ErrorMessage}")]
    partial void LogRowError(int rowNumber, string errorMessage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Batch registration completed: {Total} rows, {Success} success, {Skipped} skipped, {Errors} errors")]
    partial void LogBatchCompleted(int total, int success, int skipped, int errors);
}
