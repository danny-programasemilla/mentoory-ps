using FluentAssertions;
using Mentoory.Tenant.Application.Commands.AdvanceProjectStage;
using Xunit;

namespace Mentoory.Tenant.Tests.Validators;

public class AdvanceProjectStageValidatorTests
{
    private readonly AdvanceProjectStageValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyExternalId_Fails()
    {
        var cmd = new AdvanceProjectStageCommand(Guid.Empty, 1, 1, false);
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.ProjectExternalId));
    }

    [Fact]
    public void Validate_WithNonPositiveUserId_Fails()
    {
        var cmd = new AdvanceProjectStageCommand(Guid.NewGuid(), 0, 1, false);
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.ActingUserId));
    }

    [Fact]
    public void Validate_WithValidFields_Passes()
    {
        var cmd = new AdvanceProjectStageCommand(Guid.NewGuid(), 10, 5, false);
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }
}
