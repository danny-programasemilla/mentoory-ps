using FluentAssertions;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Mentoory.Access.Tests.Handlers;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUserProvisioningService> _provisioning = new();
    private readonly Mock<ILogger<RegisterUserHandler>> _logger = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _handler = new RegisterUserHandler(_provisioning.Object, _logger.Object);
    }

    [Theory]
    [InlineData(UserProvisioningOutcome.Success)]
    [InlineData(UserProvisioningOutcome.DuplicateEmail)]
    [InlineData(UserProvisioningOutcome.DuplicateNationalId)]
    [Trait("Spec", "FR-016-01")]
    [Trait("Spec", "FR-016-03")]
    [Trait("Spec", "FR-016-04")]
    [Trait("Floor", "response-indistinguishability")]
    public async Task Handle_AllOutcomes_ReturnSuccess_ToCloseEnumerationOracle(UserProvisioningOutcome outcome)
    {
        _provisioning.Setup(p => p.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outcome);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _provisioning.Verify(p => p.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(UserProvisioningOutcome.Success)]
    [InlineData(UserProvisioningOutcome.DuplicateEmail)]
    [InlineData(UserProvisioningOutcome.DuplicateNationalId)]
    [Trait("Spec", "FR-016-06")]
    [Trait("Floor", "outcome-audit-logging")]
    public async Task Handle_LogsOutcome_WithCorrelationAndClientIp(UserProvisioningOutcome outcome)
    {
        _provisioning.Setup(p => p.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outcome);

        await _handler.Handle(Command("corr-42", "203.0.113.9"), CancellationToken.None);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains($"Outcome: {outcome}")
                    && state.ToString()!.Contains("CorrelationId: corr-42")
                    && state.ToString()!.Contains("ClientIp: 203.0.113.9")
                    && state.ToString()!.Contains("Email: test@example.com")),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    [Trait("Spec", "FR-016-05")]
    public async Task Handle_ForwardsCommandFields_ToProvisioningRequest()
    {
        UserProvisioningRequest? captured = null;
        _provisioning
            .Setup(p => p.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UserProvisioningRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(UserProvisioningOutcome.Success);

        await _handler.Handle(Command(), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Email.Should().Be("test@example.com");
        captured.Country.Should().Be("CO");
        captured.NationalId.Should().Be("123456");
        captured.FirstName.Should().Be("Juan");
        captured.LastName.Should().Be("Pérez");
        captured.Password.Should().Be("SecureP@ss123!");
    }

    [Fact]
    [Trait("Spec", "FR-016-06")]
    [Trait("Floor", "outcome-audit-logging")]
    public async Task Handle_NullCorrelationAndIp_AreLoggedAsEmptyStrings()
    {
        _provisioning.Setup(p => p.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProvisioningOutcome.Success);

        var command = Command(correlationId: null, clientIp: null);
        await _handler.Handle(command, CancellationToken.None);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("CorrelationId: ")
                    && state.ToString()!.Contains("ClientIp: ")),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    private static RegisterUserCommand Command(string? correlationId = "trace-1", string? clientIp = "127.0.0.1") =>
        new("test@example.com", "CO", "123456", "Juan", "Pérez", "SecureP@ss123!", correlationId, clientIp);
}
