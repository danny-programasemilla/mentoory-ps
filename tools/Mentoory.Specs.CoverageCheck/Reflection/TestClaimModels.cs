namespace Mentoory.Specs.CoverageCheck.Reflection;

/// <summary>
/// Discriminator for the four trait keys the coverage tool recognises, plus an
/// <see cref="Other"/> bucket for any key it deliberately ignores.
/// </summary>
public enum TraitKind
{
    /// <summary>An xUnit <c>[Trait("Spec","FR-DDD"|"FR-DDD-DD")]</c>.</summary>
    Spec,

    /// <summary>An xUnit <c>[Trait("Sc","SC-DDD"|"SC-DDD-DD")]</c>.</summary>
    Sc,

    /// <summary>An xUnit <c>[Trait("Floor","&lt;category&gt;")]</c>.</summary>
    Floor,

    /// <summary>An xUnit <c>[Trait("Flaky","true")]</c> quarantine marker.</summary>
    Flaky,

    /// <summary>Any other trait key — counted but never claim-bearing.</summary>
    Other,
}

/// <summary>
/// A single <c>[Trait(key, value)]</c> attribute instance read off a test method.
/// </summary>
public sealed record TestClaim(TraitKind Kind, string Key, string Value, TestMethodMetadata Method);

/// <summary>
/// A test method discovered inside a test assembly. Methods carrying a non-null
/// <c>Skip</c> argument on <c>[Fact]</c>/<c>[Theory]</c> are flagged via
/// <see cref="IsSkipped"/> and are non-claiming for every trait they bear.
/// </summary>
public sealed record TestMethodMetadata(
    string DeclaringTypeFullName,
    string MethodName,
    bool IsSkipped)
{
    /// <summary>Trait claims attached to this method. Mutated only during construction.</summary>
    public List<TestClaim> TraitClaims { get; } = new();

    /// <summary>Fully-qualified <c>Namespace.Class.Method</c> name used in reports.</summary>
    public string FullyQualifiedName => $"{DeclaringTypeFullName}.{MethodName}";

    /// <summary>True when any trait key on this method is <c>Flaky</c> with literal value <c>true</c>.</summary>
    public bool IsQuarantined => TraitClaims.Any(c =>
        c.Kind == TraitKind.Flaky && string.Equals(c.Value, "true", StringComparison.Ordinal));

    /// <summary>True when the test contributes no claims (skipped or quarantined).</summary>
    public bool IsNonClaiming => IsSkipped || IsQuarantined;
}

/// <summary>
/// A single managed DLL discovered by the <c>--test-assemblies</c> glob.
/// </summary>
public sealed record TestAssembly(
    string Path,
    string AssemblyName,
    IReadOnlyList<TestMethodMetadata> TestMethods);

/// <summary>
/// Reported when an assembly can not be loaded or inspected by
/// <see cref="System.Reflection.MetadataLoadContext"/>. Maps to exit code 3.
/// </summary>
public sealed record ReflectionError(string AssemblyPath, string Reason);
