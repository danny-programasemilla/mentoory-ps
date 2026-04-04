using FluentAssertions;
using Mentoory.Access.Application.Commands.RegisterUser;
using Xunit;

namespace Mentoory.Access.Tests.Validators;

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    private static RegisterUserCommand ValidCommand => new(
        "test@example.com", "CO", "123456789", "Juan", "Pérez", "SecureP@ss123!");

    [Fact]
    public async Task Valid_Command_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public async Task Invalid_Email_Fails(string email)
    {
        var command = ValidCommand with { Email = email };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_Country_Fails(string country)
    {
        var command = ValidCommand with { Country = country };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_NationalId_Fails(string nationalId)
    {
        var command = ValidCommand with { NationalId = nationalId };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NationalId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_FirstName_Fails(string firstName)
    {
        var command = ValidCommand with { FirstName = firstName };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("lowercaseonly1!")]
    [InlineData("UPPERCASEONLY1!")]
    [InlineData("NoDigitsHere!!!")]
    [InlineData("NoSpecial1chars")]
    public async Task Weak_Password_Fails(string password)
    {
        var command = ValidCommand with { Password = password };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task FirstName_Over100_Fails()
    {
        var command = ValidCommand with { FirstName = new string('A', 101) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }
}
