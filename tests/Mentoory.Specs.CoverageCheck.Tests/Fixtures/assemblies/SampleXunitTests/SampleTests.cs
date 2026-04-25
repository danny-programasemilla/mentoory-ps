using Xunit;

namespace SampleXunitTests;

/// <summary>
/// Hand-curated xUnit test methods that exercise every trait scenario the
/// CoverageCheck reflector and analyzer must handle. Never executed as tests —
/// only inspected via MetadataLoadContext.
/// </summary>
public class SampleTests
{
    /// <summary>Good claim against sample-clean.md FR-100-01 — single Spec trait.</summary>
    [Fact]
    [Trait("Spec", "FR-100-01")]
    public void Claims_FR100_01()
    {
    }

    /// <summary>Dangling claim — no fixture spec declares FR-999.</summary>
    [Fact]
    [Trait("Spec", "FR-999")]
    public void Dangling_FR999()
    {
    }

    /// <summary>Skipped test bearing FR-100-02 — must be treated as non-claiming.</summary>
    [Fact(Skip = "demo skip — non-claiming on purpose")]
    [Trait("Spec", "FR-100-02")]
    public void Skipped_FR100_02()
    {
    }

    /// <summary>
    /// Multi-trait method: claims both FR-100-01 (already claimed elsewhere) and
    /// FR-100-02 (only otherwise-claimed by the skipped non-claiming test). Also
    /// carries a Floor trait that lands in the canonical content-policy-rules bucket.
    /// </summary>
    [Fact]
    [Trait("Spec", "FR-100-01")]
    [Trait("Spec", "FR-100-02")]
    [Trait("Floor", "content-policy-rules")]
    public void MultiClaim_FR100_01_And_FR100_02()
    {
    }

    /// <summary>Quarantined-flaky test — non-claiming, so FR-100-03 stays unclaimed.</summary>
    [Fact]
    [Trait("Flaky", "true")]
    [Trait("Spec", "FR-100-03")]
    public void Quarantined_FR100_03()
    {
    }

    /// <summary>Claims SC-100-01 to exercise the Sc trait kind path.</summary>
    [Fact]
    [Trait("Sc", "SC-100-01")]
    public void Claims_SC100_01()
    {
    }

    /// <summary>Carries a trait with an unrecognised key — must classify as Other.</summary>
    [Fact]
    [Trait("Category", "smoke")]
    public void Other_TraitKey()
    {
    }
}
