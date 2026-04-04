using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.ReadModels;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.RegisterUser;

/// <summary>
/// Handles the registration of a new user account.
/// Creates both the User aggregate and the UserProfile read model
/// within the same bounded context.
/// </summary>
public partial class RegisterUserHandler : BaseCommandHandler<RegisterUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<RegisterUserHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterUserHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <param name="userProfileRepository">The user profile repository for creating the read model.</param>
    /// <param name="passwordHasher">The password hasher for securing credentials.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
    public RegisterUserHandler(
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ILogger<RegisterUserHandler> logger)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var utcNow = _timeProvider.UtcNow;

        // Check uniqueness (generic error for enumeration prevention)
        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken) ||
            await _userRepository.ExistsByNationalIdentityAsync(request.Country, request.NationalId, cancellationToken))
        {
            return Failure(ResultErrorCodes.GenericError, ("Registration", "No fue posible completar el registro."));
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

        _userRepository.Add(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Create UserProfile read model directly (previously done via UserRegisteredEvent handler)
        var profile = UserProfile.Create(
            user.Id,
            user.ExternalId,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.AccountStatus.ToString(),
            user.CreatedAtUtc,
            utcNow);

        _userProfileRepository.Add(profile);
        await _userProfileRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        LogUserRegistered(user.Email.Value);

        return Success();
    }

    /// <summary>
    /// Logs when a user is successfully registered.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "User registered successfully. Email: {Email}")]
    partial void LogUserRegistered(string email);
}
