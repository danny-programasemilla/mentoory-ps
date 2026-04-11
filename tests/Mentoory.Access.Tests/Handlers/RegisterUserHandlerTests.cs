using FluentAssertions;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Access.Tests.Handlers;

public class RegisterUserHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<ISystemConfigurationReader> _configReader = new();
    private readonly Mock<IUnitOfWork> _userUnitOfWork = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _passwordHasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed-pw");
        _userRepo.Setup(r => r.UnitOfWork).Returns(_userUnitOfWork.Object);
        _userUnitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _configReader.Setup(c => c.GetIntAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(24);

        _handler = new RegisterUserHandler(
            _userRepo.Object,
            _passwordHasher.Object,
            _timeProvider.Object,
            _configReader.Object,
            NullLogger<RegisterUserHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByNationalIdentityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.Add(It.IsAny<User>())).Returns((User u) => u);

        var command = new RegisterUserCommand("test@test.com", "CO", "123456", "Juan", "Pérez", "SecureP@ss123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userRepo.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
        _userUnitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ReturnsFieldSpecificError()
    {
        _userRepo.Setup(r => r.ExistsByNationalIdentityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByEmailAsync("TEST@TEST.COM", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new RegisterUserCommand("test@test.com", "CO", "123456", "Juan", "Pérez", "SecureP@ss123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        result.ErrorMessages.Should().Contain(e => e.Context == "Email");
        _userRepo.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDuplicateNationalId_ReturnsFieldSpecificError()
    {
        _userRepo.Setup(r => r.ExistsByNationalIdentityAsync("CO", "123456", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new RegisterUserCommand("test@test.com", "CO", "123456", "Juan", "Pérez", "SecureP@ss123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().Contain(e => e.Context == "NationalId");
    }

    [Fact]
    public async Task Handle_HashesPassword_BeforeCreatingUser()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByNationalIdentityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.Add(It.IsAny<User>())).Returns((User u) => u);

        var command = new RegisterUserCommand("test@test.com", "CO", "123456", "Juan", "Pérez", "SecureP@ss123!");
        await _handler.Handle(command, CancellationToken.None);

        _passwordHasher.Verify(h => h.HashPassword("SecureP@ss123!"), Times.Once);
    }
}
