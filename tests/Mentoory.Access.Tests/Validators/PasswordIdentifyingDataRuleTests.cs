using FluentAssertions;
using Mentoory.Access.Application.Validation;
using Xunit;

namespace Mentoory.Access.Tests.Validators;

public class PasswordIdentifyingDataRuleTests
{
    [Fact]
    [Trait("Spec", "FR-016-11")]
    [Trait("Spec", "FR-016-14")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public void Contains_Returns_True_When_Password_Contains_Full_Email()
    {
        var matched = PasswordIdentifyingDataRule.Contains(
            "My-jane@example.com-Password1!", "jane@example.com", "123-456");

        matched.Should().BeTrue();
    }

    [Fact]
    [Trait("Spec", "FR-016-11")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public void Contains_Is_Case_Insensitive_For_Email()
    {
        var matched = PasswordIdentifyingDataRule.Contains(
            "prefixJANE@EXAMPLE.COMsuffix1!", "jane@example.com", "X");

        matched.Should().BeTrue();
    }

    [Fact]
    [Trait("Spec", "FR-016-11")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public void Contains_Returns_True_When_Local_Part_Is_At_Least_Four_Chars()
    {
        var matched = PasswordIdentifyingDataRule.Contains(
            "My-jane.doe-passw0rd!", "jane.doe@example.com", "Z");

        matched.Should().BeTrue();
    }

    [Fact]
    [Trait("Spec", "FR-016-11")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public void Contains_Skips_Local_Part_Below_Minimum_Length()
    {
        var matched = PasswordIdentifyingDataRule.Contains(
            "CorrectHorseBattery9!", "abc@example.com", "Z");

        matched.Should().BeFalse();
    }

    [Fact]
    [Trait("Spec", "FR-016-12")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public void Contains_Returns_True_When_National_Id_Verbatim_Matches()
    {
        var matched = PasswordIdentifyingDataRule.Contains(
            "Secure9-123-4567!", "fresh@example.com", "9-123-4567");

        matched.Should().BeTrue();
    }

    [Fact]
    [Trait("Spec", "FR-016-12")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public void Contains_Returns_True_When_National_Id_Stripped_Matches()
    {
        var matched = PasswordIdentifyingDataRule.Contains(
            "Secure91234567!", "fresh@example.com", "9-123-4567");

        matched.Should().BeTrue();
    }

    [Fact]
    [Trait("Spec", "FR-016-12")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public void Contains_Skips_National_Id_Below_Minimum_Length()
    {
        var matched = PasswordIdentifyingDataRule.Contains(
            "Bicycle91TestingA!", "fresh@example.com", "91");

        matched.Should().BeFalse();
    }

    [Fact]
    [Trait("Spec", "FR-016-12")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public void Contains_Skips_Stripped_National_Id_When_Below_Minimum_Length()
    {
        // stripped form of "1-2-3" is "123" (3 chars, below threshold) — must not match.
        var matched = PasswordIdentifyingDataRule.Contains(
            "Pa123wordLongTest!", "fresh@example.com", "1-2-3");

        matched.Should().BeFalse();
    }

    [Fact]
    [Trait("Spec", "FR-016-12")]
    [Trait("Floor", "content-policy-rules")]
    public void Contains_Is_Case_Sensitive_For_National_Id()
    {
        // National ID is stored as-given; compare ordinally. Lowercase password form must not match uppercase-stored ID.
        var matched = PasswordIdentifyingDataRule.Contains(
            "Secureabc1234567!", "fresh@example.com", "ABC1234567");

        matched.Should().BeFalse();
    }

    [Fact]
    [Trait("Spec", "FR-016-11")]
    [Trait("Spec", "FR-016-12")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public void Contains_Accepts_Password_Without_Identifying_Data()
    {
        var matched = PasswordIdentifyingDataRule.Contains(
            "CorrectHorseBatteryStaple9!", "jane.doe@example.com", "9-123-4567");

        matched.Should().BeFalse();
    }

    [Fact]
    [Trait("Spec", "FR-016-11")]
    [Trait("Sc", "SC-016-04")]
    [Trait("Floor", "content-policy-rules")]
    public void Contains_Full_Email_Check_Applies_Even_When_Local_Part_Below_Threshold()
    {
        // Local part "a" is below threshold; the full email "a@b.co" is still checked.
        var matched = PasswordIdentifyingDataRule.Contains(
            "MyPasswordContainsa@b.co1!", "a@b.co", "Z");

        matched.Should().BeTrue();
    }
}
