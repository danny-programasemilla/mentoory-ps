using FluentAssertions;
using Mentoory.Access.Application.Services;
using Mentoory.Access.Contracts.IntegrationEvents;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Access.Application.Configuration;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Access.Tests.Services;

public class UserRegistrationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<ISystemConfigurationReader> _configReader = new();
    private readonly Mock<IIntegrationEventService> _eventService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UserRegistrationService _service;

    public UserRegistrationServiceTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _passwordHasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed-pw");
        _userRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _configReader.Setup(c => c.GetIntAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(24);
        _userRepo.Setup(r => r.Add(It.IsAny<User>())).Returns((User u) => u);

        _service = new UserRegistrationService(
            _userRepo.Object,
            _passwordHasher.Object,
            _timeProvider.Object,
            _configReader.Object,
            _eventService.Object,
            NullLogger<UserRegistrationService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserAndPublishesEvent()
    {
        SetupNoExistingUsers();

        var result = await _service.RegisterAsync(CreateValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be("test@test.com");
        _userRepo.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventService.Verify(e => e.PublishAsync(It.IsAny<UserRegisteredEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsFailureWithEmailField()
    {
        _userRepo.Setup(r => r.ExistsByNationalIdentityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByEmailAsync("TEST@TEST.COM", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.RegisterAsync(CreateValidRequest(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        result.ErrorMessages.Should().Contain(e => e.Context == "Email");
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateNationalId_ReturnsFailureWithNationalIdField()
    {
        _userRepo.Setup(r => r.ExistsByNationalIdentityAsync("CO", "123456", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.RegisterAsync(CreateValidRequest(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().Contain(e => e.Context == "NationalId");
    }

    [Fact]
    public async Task RegisterAsync_HashesPasswordBeforeRegister()
    {
        SetupNoExistingUsers();

        await _service.RegisterAsync(CreateValidRequest(), CancellationToken.None);

        _passwordHasher.Verify(h => h.HashPassword("SecureP@ss123!"), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenVerificationRequired_GeneratesToken()
    {
        SetupNoExistingUsers();

        var result = await _service.RegisterAsync(
            CreateValidRequest(EmailVerificationMode.Required), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccountStatus.Should().Be("PendingVerification");
        // Verify token hash was generated (2 calls: one for password, one for token)
        _passwordHasher.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RegisterAsync_WhenVerificationSkipped_AdminVerifiesEmail()
    {
        SetupNoExistingUsers();

        var result = await _service.RegisterAsync(
            CreateValidRequest(EmailVerificationMode.Skipped), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccountStatus.Should().Be("Active");
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordResetRequired_SetsFlag()
    {
        SetupNoExistingUsers();

        var result = await _service.RegisterAsync(
            CreateValidRequest(EmailVerificationMode.Skipped, requirePasswordReset: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccountStatus.Should().Be("PasswordResetRequired");
    }

    [Fact]
    public async Task RegisterAsync_PublishesEventWithCorrectProjectAndVariant()
    {
        SetupNoExistingUsers();
        var projectId = Guid.NewGuid();

        await _service.RegisterAsync(
            CreateValidRequest(projectExternalId: projectId, variant: "FullFlow"), CancellationToken.None);

        _eventService.Verify(e => e.PublishAsync(
            It.Is<UserRegisteredEvent>(evt =>
                evt.ProjectExternalId == projectId &&
                evt.EnrollmentVariant == "FullFlow"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static UserRegistrationRequest CreateValidRequest(
        EmailVerificationMode mode = EmailVerificationMode.Required,
        Guid? projectExternalId = null,
        string variant = "SelfRegistration",
        bool requirePasswordReset = false)
    {
        return new UserRegistrationRequest(
            "test@test.com",
            "CO",
            "123456",
            "Juan",
            "Pérez",
            "SecureP@ss123!",
            projectExternalId,
            mode,
            variant,
            requirePasswordReset);
    }

    private void SetupNoExistingUsers()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByNationalIdentityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
    }
}
