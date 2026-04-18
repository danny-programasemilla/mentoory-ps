using FluentAssertions;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Application.Infrastructure;
using Mentoory.Access.Application.Services;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Moq;
using Xunit;

namespace Mentoory.Access.Tests.Services;

public class UserProvisioningServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<ISystemConfigurationReader> _configReader = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UserProvisioningService _service;

    public UserProvisioningServiceTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _passwordHasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed");
        _userRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _configReader.Setup(c => c.GetIntAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(24);

        _service = new UserProvisioningService(
            _userRepo.Object,
            _passwordHasher.Object,
            _timeProvider.Object,
            _configReader.Object);
    }

    private static UserProvisioningRequest ValidRequest => new(
        "test@example.com", "CO", "123456789", "Juan", "Pérez", "SecureP@ss123!");

    [Fact]
    public async Task ProvisionAsync_FreshCombination_ReturnsSuccess_AndPersists()
    {
        _userRepo.Setup(r => r.ExistsByNationalIdentityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepo.Setup(r => r.Add(It.IsAny<User>())).Returns((User u) => u);

        var outcome = await _service.ProvisionAsync(ValidRequest, CancellationToken.None);

        outcome.Should().Be(UserProvisioningOutcome.Success);
        _userRepo.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProvisionAsync_DuplicateNationalId_ReturnsEarly_WithZeroWrites()
    {
        _userRepo.Setup(r => r.ExistsByNationalIdentityAsync("CO", "123456789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var outcome = await _service.ProvisionAsync(ValidRequest, CancellationToken.None);

        outcome.Should().Be(UserProvisioningOutcome.DuplicateNationalId);
        _userRepo.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _userRepo.Verify(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProvisionAsync_DuplicateEmail_ReturnsEarly_WithZeroWrites()
    {
        _userRepo.Setup(r => r.ExistsByNationalIdentityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByEmailAsync("TEST@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var outcome = await _service.ProvisionAsync(ValidRequest, CancellationToken.None);

        outcome.Should().Be(UserProvisioningOutcome.DuplicateEmail);
        _userRepo.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProvisionAsync_Checks_NationalId_Before_Email()
    {
        var sequence = new MockSequence();
        _userRepo.InSequence(sequence)
            .Setup(r => r.ExistsByNationalIdentityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepo.InSequence(sequence)
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepo.Setup(r => r.Add(It.IsAny<User>())).Returns((User u) => u);

        await _service.ProvisionAsync(ValidRequest, CancellationToken.None);

        _userRepo.Verify(r => r.ExistsByNationalIdentityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.Verify(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
