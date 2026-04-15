using FluentAssertions;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Application.Services;
using Mentoory.Shared.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Access.Tests.Handlers;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUserRegistrationService> _registrationService = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(
            _registrationService.Object,
            NullLogger<RegisterUserHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ConstructsCorrectRequest_AndDelegatesToService()
    {
        var serviceResult = new UserRegistrationResult(1, Guid.NewGuid(), "test@test.com", "PendingVerification", DateTime.UtcNow);
        _registrationService
            .Setup(s => s.RegisterAsync(It.IsAny<UserRegistrationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(serviceResult));

        var command = new RegisterUserCommand("test@test.com", "CO", "123456", "Juan", "Pérez", "SecureP@ss123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _registrationService.Verify(s => s.RegisterAsync(
            It.Is<UserRegistrationRequest>(r =>
                r.Email == "test@test.com" &&
                r.Country == "CO" &&
                r.NationalId == "123456" &&
                r.FirstName == "Juan" &&
                r.LastName == "Pérez" &&
                r.Password == "SecureP@ss123!" &&
                r.ProjectExternalId == null &&
                r.EmailVerificationMode == EmailVerificationMode.Required &&
                r.EnrollmentVariant == "SelfRegistration" &&
                r.RequirePasswordReset == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceFails_PropagatesFailure()
    {
        _registrationService
            .Setup(s => s.RegisterAsync(It.IsAny<UserRegistrationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserRegistrationResult>.Failure(
                ResultErrorCodes.GenericError,
                ("Email", "Ya existe una cuenta con este correo electrónico.")));

        var command = new RegisterUserCommand("test@test.com", "CO", "123456", "Juan", "Pérez", "SecureP@ss123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        result.ErrorMessages.Should().Contain(e => e.Context == "Email");
    }

    [Fact]
    public async Task Handle_WhenServiceSucceeds_ReturnsSuccess()
    {
        var serviceResult = new UserRegistrationResult(1, Guid.NewGuid(), "test@test.com", "PendingVerification", DateTime.UtcNow);
        _registrationService
            .Setup(s => s.RegisterAsync(It.IsAny<UserRegistrationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(serviceResult));

        var command = new RegisterUserCommand("test@test.com", "CO", "123456", "Juan", "Pérez", "SecureP@ss123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
