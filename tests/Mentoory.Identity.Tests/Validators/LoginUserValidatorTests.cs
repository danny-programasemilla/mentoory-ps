using FluentAssertions;
using Mentoory.Identity.Application.Commands.LoginUser;
using Xunit;

namespace Mentoory.Identity.Tests.Validators;

public class LoginUserValidatorTests
{
    private readonly LoginUserValidator _validator = new();

    private static LoginUserCommand ValidCommand => new(
        "test@example.com", "SecureP@ss123!", "127.0.0.1", "TestBrowser");

    [Fact]
    public async Task Valid_Command_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Empty_Email_Fails()
    {
        var command = ValidCommand with { Email = string.Empty };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Empty_Password_Fails()
    {
        var command = ValidCommand with { Password = string.Empty };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Empty_IpAddress_Fails()
    {
        var command = ValidCommand with { IpAddress = string.Empty };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IpAddress");
    }

    [Fact]
    public async Task Null_UserAgent_IsAllowed()
    {
        var command = ValidCommand with { UserAgent = null };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
