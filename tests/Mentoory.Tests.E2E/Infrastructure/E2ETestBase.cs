using Xunit;

namespace Mentoory.Tests.E2E.Infrastructure;

/// <summary>
/// Base class for E2E tests that require a clean database before each test method.
/// Inheriting tests must use <c>[Collection(E2ETestCollection.Name)]</c> and pass
/// <see cref="PlaywrightFixture"/> via constructor.
/// </summary>
public abstract class E2ETestBase : IAsyncLifetime
{
    protected E2ETestBase(PlaywrightFixture fixture)
    {
        Fixture = fixture;
    }

    protected PlaywrightFixture Fixture { get; }

    public Task InitializeAsync() => Fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
