using Xunit;

namespace Mentoory.Tests.E2E.Infrastructure;

[CollectionDefinition(Name)]
public class E2ETestCollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "E2E";
}
