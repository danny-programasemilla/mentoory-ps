using FluentAssertions;
using Mentoory.Identity.Application.Commands.RegisterUser;
using Mentoory.Identity.Application.IntegrationEvents;
using Mentoory.Identity.Domain.Aggregates.User;
using Mentoory.Identity.Domain.Enums;
using Mentoory.Identity.Domain.Repositories;
using Mentoory.Identity.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Identity.Tests.Handlers;

public class RegisterUserHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IIntegrationEventService> _eventService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _passwordHasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed-pw");
        _userRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _handler = new RegisterUserHandler(
            _userRepo.Object,
            _passwordHasher.Object,
            _timeProvider.Object,
            _eventService.Object,
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
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventService.Verify(e => e.PublishAsync(It.IsAny<UserRegisteredEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ReturnsFailure()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync("TEST@TEST.COM", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new RegisterUserCommand("test@test.com", "CO", "123456", "Juan", "Pérez", "SecureP@ss123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        _userRepo.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDuplicateNationalId_ReturnsFailure()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByNationalIdentityAsync("CO", "123456", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new RegisterUserCommand("test@test.com", "CO", "123456", "Juan", "Pérez", "SecureP@ss123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
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
