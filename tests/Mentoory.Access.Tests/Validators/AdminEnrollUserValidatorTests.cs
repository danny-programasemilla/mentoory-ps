using FluentAssertions;
using Mentoory.Access.Application.Commands.AdminEnrollUser;
using Mentoory.Access.Application.Validation;
using Xunit;

namespace Mentoory.Access.Tests.Validators;

public class AdminEnrollUserValidatorTests
{
    private readonly AdminEnrollUserValidator _validator = new();

    private static AdminEnrollUserCommand ValidCommand => new(
        "admin@example.com", "CO", "99887766", "Ana", "Gómez", "SecureP@ss12345!");

    [Fact]
    [Trait("Spec", "FR-016-10")]
    public async Task Valid_Command_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Spec", "FR-016-10")]
    [Trait("Spec", "FR-016-11")]
    [Trait("Spec", "FR-016-13")]
    [Trait("Spec", "FR-016-14")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public async Task Password_Containing_Email_LocalPart_Fails_With_Shared_Message()
    {
        // Local part "admin" is 5 chars ≥ 4, should trigger MustNotContainIdentifyingData.
        var command = ValidCommand with { Password = "ContainsAdmin-Password9!" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password"
            && e.ErrorMessage == PasswordIdentifyingDataRule.Message);
    }

    [Fact]
    [Trait("Spec", "FR-016-10")]
    [Trait("Spec", "FR-016-12")]
    [Trait("Spec", "FR-016-13")]
    [Trait("Spec", "FR-016-14")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public async Task Password_Containing_National_Id_Fails_With_Shared_Message()
    {
        var command = ValidCommand with { Password = "Secure99887766!" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password"
            && e.ErrorMessage == PasswordIdentifyingDataRule.Message);
    }
}
