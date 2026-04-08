using System.Security.Cryptography;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.RegenerateVerificationToken;

public partial class RegenerateVerificationTokenHandler : BaseCommandHandler<RegenerateVerificationTokenCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ISystemConfigurationReader _configReader;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<RegenerateVerificationTokenHandler> _logger;

    public RegenerateVerificationTokenHandler(
        IUserRepository userRepository,
        ISystemConfigurationReader configReader,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ILogger<RegenerateVerificationTokenHandler> logger)
    {
        _userRepository = userRepository;
        _configReader = configReader;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override async Task<Result> Handle(RegenerateVerificationTokenCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.UtcNow;

        var user = await _userRepository.GetByExternalIdAsync(request.UserExternalId, cancellationToken);
        if (user is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("RegenerateToken", "Usuario no encontrado."));
        }

        var expiryHours = await _configReader.GetIntAsync(
            nameof(ConfigurationKey.EmailVerificationTokenExpiryHours), cancellationToken);

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenHash = _passwordHasher.HashPassword(Convert.ToBase64String(tokenBytes));

        user.GenerateEmailVerificationToken(utcNow, tokenHash, expiryHours);

        _userRepository.Update(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogTokenRegenerated(user.Email.Value);

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Verification token regenerated for user: {Email}")]
    partial void LogTokenRegenerated(string email);
}
