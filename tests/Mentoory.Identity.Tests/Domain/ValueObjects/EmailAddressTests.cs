using FluentAssertions;
using Mentoory.Identity.Domain.ValueObjects;
using Xunit;

namespace Mentoory.Identity.Tests.Domain.ValueObjects;

public class EmailAddressTests
{
    [Fact]
    public void Constructor_ShouldNormalizeEmail()
    {
        var email = new EmailAddress("Test@Example.COM");
        email.NormalizedValue.Should().Be("TEST@EXAMPLE.COM");
        email.Value.Should().Be("Test@Example.COM");
    }

    [Fact]
    public void Constructor_WithEmptyEmail_ShouldThrow()
    {
        var act = () => new EmailAddress(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_ShouldBeCaseInsensitive()
    {
        var email1 = new EmailAddress("test@test.com");
        var email2 = new EmailAddress("TEST@TEST.COM");
        email1.Should().Be(email2);
    }
}
