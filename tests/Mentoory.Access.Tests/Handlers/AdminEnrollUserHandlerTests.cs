using FluentAssertions;
using Mentoory.Access.Application.Commands.AdminEnrollUser;
using Mentoory.Access.Application.Services;
using Mentoory.Shared.Application;
using Moq;
using Xunit;

namespace Mentoory.Access.Tests.Handlers;

public class AdminEnrollUserHandlerTests
{
    private readonly Mock<IUserProvisioningService> _provisioning = new();
    private readonly AdminEnrollUserHandler _handler;

    public AdminEnrollUserHandlerTests()
    {
        _handler = new AdminEnrollUserHandler(_provisioning.Object);
    }

    [Fact]
    public async Task Handle_Fresh_ReturnsSuccess()
    {
        _provisioning.Setup(p => p.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProvisioningOutcome.Success);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure_AttributedToEmail()
    {
        _provisioning.Setup(p => p.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProvisioningOutcome.DuplicateEmail);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        result.ErrorMessages.Should().ContainSingle(e => e.Context == "Email"
            && e.Message == "Ya existe una cuenta con este correo electrónico.");
    }

    [Fact]
    public async Task Handle_DuplicateNationalId_ReturnsFailure_AttributedToNationalId()
    {
        _provisioning.Setup(p => p.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProvisioningOutcome.DuplicateNationalId);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        result.ErrorMessages.Should().ContainSingle(e => e.Context == "NationalId"
            && e.Message == "Ya existe una cuenta con este número de identificación.");
    }

    [Fact]
    public async Task Handle_ForwardsAllFields_ToProvisioningRequest()
    {
        UserProvisioningRequest? captured = null;
        _provisioning
            .Setup(p => p.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UserProvisioningRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(UserProvisioningOutcome.Success);

        await _handler.Handle(Command(), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Email.Should().Be("admin@example.com");
        captured.Country.Should().Be("CO");
        captured.NationalId.Should().Be("99887766");
        captured.FirstName.Should().Be("Ana");
        captured.LastName.Should().Be("Gómez");
        captured.Password.Should().Be("AdminP@ss12345!");
    }

    private static AdminEnrollUserCommand Command() =>
        new("admin@example.com", "CO", "99887766", "Ana", "Gómez", "AdminP@ss12345!");
}
