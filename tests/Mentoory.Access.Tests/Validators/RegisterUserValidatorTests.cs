using FluentAssertions;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Application.Validation;
using Xunit;

namespace Mentoory.Access.Tests.Validators;

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    private static RegisterUserCommand ValidCommand => new(
        "jane.doe@example.com", "CO", "9-123-4567", "Juan", "Pérez", "SecureP@ss123!");

    [Fact]
    [Trait("Spec", "FR-016-10")]
    public async Task Valid_Command_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [Trait("Spec", "FR-016-01")]
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
    [Trait("Spec", "FR-016-01")]
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
    [Trait("Spec", "FR-016-01")]
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
    [Trait("Spec", "FR-016-01")]
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
    [Trait("Spec", "FR-016-01")]
    public async Task Weak_Password_Fails(string password)
    {
        var command = ValidCommand with { Password = password };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    [Trait("Spec", "FR-016-01")]
    public async Task FirstName_Over100_Fails()
    {
        var command = ValidCommand with { FirstName = new string('A', 101) };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("jane.doe@example.com-Password1!")] // full email match
    [InlineData("Myjane.doePassword1!")] // local-part match (>= 4 chars)
    [InlineData("Secure9-123-4567Pass!")] // verbatim national-ID match
    [InlineData("Secure91234567Pass!")] // stripped national-ID match
    [Trait("Spec", "FR-016-11")]
    [Trait("Spec", "FR-016-12")]
    [Trait("Spec", "FR-016-13")]
    [Trait("Spec", "FR-016-14")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public async Task Password_Containing_Identifying_Data_Fails_With_Shared_Message(string password)
    {
        var command = ValidCommand with { Password = password };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password"
            && e.ErrorMessage == PasswordIdentifyingDataRule.Message);
    }

    [Fact]
    [Trait("Spec", "FR-016-11")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public async Task Password_With_Below_Threshold_Local_Part_Is_Accepted()
    {
        // Local part "abc" is below 4-char threshold → identifying-data check must skip it.
        // Full-email check still applies, but the password does not contain "abc@x.co".
        var command = ValidCommand with
        {
            Email = "abc@x.co",
            Password = "CorrectHorseBattery9!",
        };

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().NotContain(e => e.PropertyName == "Password"
            && e.ErrorMessage == PasswordIdentifyingDataRule.Message);
    }

    [Fact]
    [Trait("Spec", "FR-016-12")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public async Task Password_With_Below_Threshold_National_Id_Is_Accepted()
    {
        // National ID "91" is below 4-char threshold; stripped form also < 4.
        var command = ValidCommand with
        {
            NationalId = "91",
            Password = "Bicycle91TestingA!",
        };

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().NotContain(e => e.PropertyName == "Password"
            && e.ErrorMessage == PasswordIdentifyingDataRule.Message);
    }
}
