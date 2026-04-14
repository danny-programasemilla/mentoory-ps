using FluentAssertions;
using Mentoory.Access.Application.Commands.LoginUser;
using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Domain.Aggregates.AuthSession;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Access.Tests.Handlers;

public class LoginUserHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IAuthSessionRepository> _sessionRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISystemConfigurationReader> _configReader = new();
    private readonly Mock<IIntegrationEventService> _eventService = new();
    private readonly LoginUserHandler _handler;

    public LoginUserHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _userRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _sessionRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _sessionRepo.Setup(r => r.GetActiveSessionsByUserIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthSession>());
        _sessionRepo.Setup(r => r.Add(It.IsAny<AuthSession>())).Returns((AuthSession s) => s);
        _configReader.Setup(r => r.GetIntAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) => key switch
            {
                nameof(ConfigurationKey.MaxFailedLoginAttempts) => 5,
                nameof(ConfigurationKey.LockoutDurationMinutes) => 15,
                nameof(ConfigurationKey.SessionTimeoutHours) => 8,
                _ => 0
            });

        _handler = new LoginUserHandler(
            _userRepo.Object,
            _sessionRepo.Object,
            _passwordHasher.Object,
            _timeProvider.Object,
            _configReader.Object,
            _eventService.Object,
            NullLogger<LoginUserHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsFailure()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new LoginUserCommand("nobody@test.com", "password", "127.0.0.1", "Agent");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
    }

    [Fact]
    public async Task Handle_WithLockedUser_ReturnsFailure()
    {
        var user = User.Register("test@test.com", "CO", "123", "A", "B", "hash", UtcNow);
        user.VerifyEmail(UtcNow);
        user.Lock(UtcNow, TimeSpan.FromMinutes(30));

        _userRepo.Setup(r => r.GetByEmailAsync("TEST@TEST.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new LoginUserCommand("test@test.com", "password", "127.0.0.1", null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithPendingVerification_ReturnsFailure()
    {
        var user = User.Register("test@test.com", "CO", "123", "A", "B", "hash", UtcNow);
        _userRepo.Setup(r => r.GetByEmailAsync("TEST@TEST.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new LoginUserCommand("test@test.com", "password", "127.0.0.1", null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithWrongPassword_IncrementsFailedAttempts()
    {
        var user = User.Register("test@test.com", "CO", "123", "A", "B", "hash", UtcNow);
        user.VerifyEmail(UtcNow);

        _userRepo.Setup(r => r.GetByEmailAsync("TEST@TEST.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword("wrong", "hash")).Returns(false);

        var command = new LoginUserCommand("test@test.com", "wrong", "127.0.0.1", null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(1);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsSession()
    {
        var user = User.Register("test@test.com", "CO", "123", "A", "B", "hash", UtcNow);
        user.VerifyEmail(UtcNow);
        _userRepo.Setup(r => r.GetByEmailAsync("TEST@TEST.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword("correct", "hash")).Returns(true);

        var command = new LoginUserCommand("test@test.com", "correct", "192.168.1.1", "TestBrowser");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Session.IpAddress.Should().Be("192.168.1.1");
        result.Value.Session.UserAgent.Should().Be("TestBrowser");
        result.Value.Session.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidCredentials_InvalidatesPreviousSessions()
    {
        var user = User.Register("test@test.com", "CO", "123", "A", "B", "hash", UtcNow);
        user.VerifyEmail(UtcNow);
        var existingSession = AuthSession.Create("old-token", 1, "10.0.0.1", null, UtcNow.AddHours(-1), TimeSpan.FromHours(8));
        _sessionRepo.Setup(r => r.GetActiveSessionsByUserIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthSession> { existingSession });
        _userRepo.Setup(r => r.GetByEmailAsync("TEST@TEST.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword("correct", "hash")).Returns(true);

        var command = new LoginUserCommand("test@test.com", "correct", "192.168.1.1", null);
        await _handler.Handle(command, CancellationToken.None);

        existingSession.IsActive.Should().BeFalse();
        _sessionRepo.Verify(r => r.Update(existingSession), Times.Once);
    }
}
