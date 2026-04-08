using System.Security.Cryptography;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.RegisterUser;

public partial class RegisterUserHandler : BaseCommandHandler<RegisterUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ISystemConfigurationReader _configReader;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ISystemConfigurationReader configReader,
        ILogger<RegisterUserHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _configReader = configReader;
        _logger = logger;
    }

    public override async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var utcNow = _timeProvider.UtcNow;

        // Split uniqueness check: country+ID first, then email (per spec FR-006, FR-007)
        if (await _userRepository.ExistsByNationalIdentityAsync(request.Country, request.NationalId, cancellationToken))
        {
            return Failure(ResultErrorCodes.GenericError, ("NationalId", "Ya existe una cuenta con este número de identificación."));
        }

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return Failure(ResultErrorCodes.GenericError, ("Email", "Ya existe una cuenta con este correo electrónico."));
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

        // Generate email verification token at registration time
        var expiryHours = await _configReader.GetIntAsync(
            nameof(ConfigurationKey.EmailVerificationTokenExpiryHours), cancellationToken);
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenHash = _passwordHasher.HashPassword(Convert.ToBase64String(tokenBytes));
        user.GenerateEmailVerificationToken(utcNow, tokenHash, expiryHours);

        _userRepository.Add(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogUserRegistered(user.Email.Value);

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User registered successfully. Email: {Email}")]
    partial void LogUserRegistered(string email);
}
