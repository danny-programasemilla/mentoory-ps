using Xunit;

namespace Mentoory.Tests.Integration.Fixtures;

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<MentooryWebApplicationFactory>
{
    public const string Name = "Integration";
}
