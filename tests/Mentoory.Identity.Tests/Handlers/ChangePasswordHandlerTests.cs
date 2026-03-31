using FluentAssertions;
using Mentoory.Identity.Application.Commands.ChangePassword;
using Mentoory.Identity.Domain.Aggregates.User;
using Mentoory.Identity.Domain.Repositories;
using Mentoory.Identity.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Identity.Tests.Handlers;

public class ChangePasswordHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ChangePasswordHandler _handler;

    public ChangePasswordHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _userRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _handler = new ChangePasswordHandler(
            _userRepo.Object,
            _passwordHasher.Object,
            _timeProvider.Object,
            NullLogger<ChangePasswordHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsFailure()
    {
        _userRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var command = new ChangePasswordCommand(999, "old", "NewP@ssword123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ReturnsFailure()
    {
        var user = User.Register("test@test.com", "CO", "123", "A", "B", "old-hash", UtcNow);
        _userRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword("wrong", "old-hash")).Returns(false);

        var command = new ChangePasswordCommand(1, "wrong", "NewP@ssword123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidData_ChangesPassword()
    {
        var user = User.Register("test@test.com", "CO", "123", "A", "B", "old-hash", UtcNow);
        _userRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword("old-pass", "old-hash")).Returns(true);
        _passwordHasher.Setup(h => h.HashPassword("NewP@ssword123!")).Returns("new-hash");

        var command = new ChangePasswordCommand(1, "old-pass", "NewP@ssword123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.GetActiveCredential()!.PasswordHash.Should().Be("new-hash");
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
