using System.Security.Cryptography;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Application.Services;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application.TimeProvider;

namespace Mentoory.Access.Application.Infrastructure;

public sealed class UserProvisioningService : IUserProvisioningService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ISystemConfigurationReader _configReader;

    public UserProvisioningService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ISystemConfigurationReader configReader)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _configReader = configReader;
    }

    public async Task<UserProvisioningOutcome> ProvisionAsync(
        UserProvisioningRequest request,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByNationalIdentityAsync(request.Country, request.NationalId, cancellationToken))
        {
            return UserProvisioningOutcome.DuplicateNationalId;
        }

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return UserProvisioningOutcome.DuplicateEmail;
        }

        var utcNow = _timeProvider.UtcNow;
        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        var user = User.Register(
            request.Email,
            request.Country,
            request.NationalId,
            request.FirstName,
            request.LastName,
            hashedPassword,
            utcNow);

        var expiryHours = await _configReader.GetIntAsync(
            nameof(ConfigurationKey.EmailVerificationTokenExpiryHours), cancellationToken);
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenHash = _passwordHasher.HashPassword(Convert.ToBase64String(tokenBytes));
        user.GenerateEmailVerificationToken(utcNow, tokenHash, expiryHours);

        _userRepository.Add(user);
        await _userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return UserProvisioningOutcome.Success;
    }
}
