using FluentAssertions;
using Mentoory.Authorization.Application.Commands.AssignRole;
using Mentoory.Shared.Domain.Constants;
using Xunit;

namespace Mentoory.Authorization.Tests.Validators;

public class AssignRoleValidatorTests
{
    private readonly AssignRoleValidator _validator = new();

    private static AssignRoleCommand ValidCommand => new(1, 10, null, Roles.Mentor);

    [Fact]
    public async Task Valid_Command_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Invalid_UserId_Fails(long userId)
    {
        var command = ValidCommand with { UserId = userId };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Invalid_IncubatorId_Fails(long incubatorId)
    {
        var command = ValidCommand with { IncubatorId = incubatorId };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IncubatorId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_Role_Fails(string role)
    {
        var command = ValidCommand with { Role = role };
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }
}
