using Mentoory.Identity.Application.IntegrationEvents;
using Mentoory.Identity.Domain.Aggregates.User;
using Mentoory.Identity.Domain.Repositories;
using Mentoory.Identity.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Identity.Application.Commands.RegisterUser;

/// <summary>
/// Handles the registration of a new user account.
/// </summary>
public partial class RegisterUserHandler : BaseCommandHandler<RegisterUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<RegisterUserHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterUserHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <param name="passwordHasher">The password hasher for securing credentials.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="eventService">The integration event service for publishing events.</param>
    /// <param name="logger">The logger instance.</param>
    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        IIntegrationEventService eventService,
        ILogger<RegisterUserHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _eventService = eventService;
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

        LogUserRegistered(user.Email.Value);

        await _eventService.PublishAsync(
            new UserRegisteredEvent(
                user.Id,
                user.ExternalId,
                user.Email.Value,
                user.FirstName,
                user.LastName,
                user.AccountStatus.ToString(),
                user.CreatedAtUtc,
                utcNow),
            cancellationToken);

        return Success();
    }

    /// <summary>
    /// Logs when a user is successfully registered.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "User registered successfully. Email: {Email}")]
    partial void LogUserRegistered(string email);
}
