using System.Reflection;
using System.Runtime.InteropServices;

namespace Mentoory.Specs.CoverageCheck.Reflection;

/// <summary>
/// Result of a single assembly inspection — either the loaded
/// <see cref="TestAssembly"/> metadata or a <see cref="ReflectionError"/>.
/// </summary>
public sealed record TraitReflectionResult(TestAssembly? Assembly, ReflectionError? Error);

/// <summary>
/// Loads test assemblies via <see cref="MetadataLoadContext"/> and enumerates
/// every test method's <c>[Trait]</c> attributes. No test code is executed.
/// </summary>
public static class TraitReflector
{
    private const string TraitAttributeFullName = "Xunit.TraitAttribute";
    private const string FactAttributeFullName = "Xunit.FactAttribute";
    private const string TheoryAttributeFullName = "Xunit.TheoryAttribute";

    /// <summary>
    /// Inspects every assembly in <paramref name="assemblyPaths"/>. Returns one
    /// result per input path in deterministic, path-sorted order.
    /// </summary>
    public static IReadOnlyList<TraitReflectionResult> Inspect(IEnumerable<string> assemblyPaths)
    {
        ArgumentNullException.ThrowIfNull(assemblyPaths);

        var paths = assemblyPaths
            .Select(System.IO.Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        if (paths.Length == 0)
        {
            return Array.Empty<TraitReflectionResult>();
        }

        var resolverDirs = BuildResolverDirectories(paths);
        var results = new List<TraitReflectionResult>(paths.Length);

        try
        {
            var resolverFiles = EnumerateResolverFiles(resolverDirs);
            var resolver = new PathAssemblyResolver(resolverFiles);
            using var context = new MetadataLoadContext(resolver);

            foreach (var path in paths)
            {
                results.Add(InspectOne(context, path));
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // If the load context itself can not be constructed, every assembly is
            // unreachable; report each one with the same reason for transparency.
            foreach (var path in paths)
            {
                results.Add(new TraitReflectionResult(
                    Assembly: null,
                    Error: new ReflectionError(path, $"Failed to initialise MetadataLoadContext: {ex.Message}")));
            }
        }

        return results;
    }

    private static TraitReflectionResult InspectOne(MetadataLoadContext context, string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            return new TraitReflectionResult(
                Assembly: null,
                Error: new ReflectionError(assemblyPath, "Assembly file not found."));
        }

        Assembly loaded;
        try
        {
            loaded = context.LoadFromAssemblyPath(assemblyPath);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new TraitReflectionResult(
                Assembly: null,
                Error: new ReflectionError(assemblyPath, $"LoadFromAssemblyPath failed: {ex.Message}"));
        }

        var assemblyName = loaded.GetName().Name ?? System.IO.Path.GetFileNameWithoutExtension(assemblyPath);

        Type[] types;
        try
        {
            types = loaded.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Some types may be unloadable (transitive dep missing); keep what we got.
            types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new TraitReflectionResult(
                Assembly: null,
                Error: new ReflectionError(assemblyPath, $"GetTypes failed: {ex.Message}"));
        }

        var methods = new List<TestMethodMetadata>();
        foreach (var type in types.OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            CollectTestMethods(type, methods);
        }

        var ordered = methods
            .OrderBy(m => m.DeclaringTypeFullName, StringComparer.Ordinal)
            .ThenBy(m => m.MethodName, StringComparer.Ordinal)
            .ToArray();

        return new TraitReflectionResult(
            Assembly: new TestAssembly(assemblyPath, assemblyName, ordered),
            Error: null);
    }

    private static void CollectTestMethods(Type type, List<TestMethodMetadata> sink)
    {
        MethodInfo[] methods;
        try
        {
            methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Unloadable type members — skip silently; the assembly-level error path is reserved for hard load failures.
            _ = ex;
            return;
        }

        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributesData();
            string? skipReason = null;
            var hasTestAttribute = false;
            var traits = new List<(string Key, string Value)>();

            foreach (var attribute in attributes)
            {
                var attributeName = SafeFullName(attribute);
                if (attributeName is null)
                {
                    continue;
                }

                if (string.Equals(attributeName, FactAttributeFullName, StringComparison.Ordinal) ||
                    string.Equals(attributeName, TheoryAttributeFullName, StringComparison.Ordinal) ||
                    InheritsFromFactOrTheory(attribute))
                {
                    hasTestAttribute = true;
                    skipReason ??= ReadSkipNamedArgument(attribute);
                }
                else if (string.Equals(attributeName, TraitAttributeFullName, StringComparison.Ordinal))
                {
                    var (key, value) = ReadTraitConstructorArguments(attribute);
                    if (key is not null && value is not null)
                    {
                        traits.Add((key, value));
                    }
                }
            }

            if (!hasTestAttribute)
            {
                continue;
            }

            var declaringName = type.FullName ?? type.Name;
            var metadata = new TestMethodMetadata(declaringName, method.Name, IsSkipped: skipReason is not null);
            foreach (var (key, value) in traits)
            {
                metadata.TraitClaims.Add(new TestClaim(ClassifyTraitKey(key), key, value, metadata));
            }

            sink.Add(metadata);
        }
    }

    private static bool InheritsFromFactOrTheory(CustomAttributeData attribute)
    {
        try
        {
            for (var t = attribute.AttributeType; t is not null; t = t.BaseType)
            {
                var name = t.FullName;
                if (string.Equals(name, FactAttributeFullName, StringComparison.Ordinal) ||
                    string.Equals(name, TheoryAttributeFullName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _ = ex;
        }

        return false;
    }

    private static string? SafeFullName(CustomAttributeData attribute)
    {
        try
        {
            return attribute.AttributeType.FullName;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _ = ex;
            return null;
        }
    }

    private static string? ReadSkipNamedArgument(CustomAttributeData attribute)
    {
        foreach (var arg in attribute.NamedArguments)
        {
            if (string.Equals(arg.MemberName, "Skip", StringComparison.Ordinal))
            {
                return arg.TypedValue.Value as string;
            }
        }

        return null;
    }

    private static (string? Key, string? Value) ReadTraitConstructorArguments(CustomAttributeData attribute)
    {
        if (attribute.ConstructorArguments.Count < 2)
        {
            return (null, null);
        }

        var key = attribute.ConstructorArguments[0].Value as string;
        var value = attribute.ConstructorArguments[1].Value as string;
        return (key, value);
    }

    private static TraitKind ClassifyTraitKey(string key)
    {
        if (string.Equals(key, "Spec", StringComparison.Ordinal))
        {
            return TraitKind.Spec;
        }

        if (string.Equals(key, "Sc", StringComparison.Ordinal))
        {
            return TraitKind.Sc;
        }

        if (string.Equals(key, "Floor", StringComparison.Ordinal))
        {
            return TraitKind.Floor;
        }

        if (string.Equals(key, "Flaky", StringComparison.Ordinal))
        {
            return TraitKind.Flaky;
        }

        return TraitKind.Other;
    }

    private static IReadOnlyList<string> BuildResolverDirectories(IReadOnlyList<string> assemblyPaths)
    {
        var dirs = new HashSet<string>(StringComparer.Ordinal)
        {
            RuntimeEnvironment.GetRuntimeDirectory(),
        };

        foreach (var path in assemblyPaths)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                dirs.Add(dir);
            }
        }

        return dirs.ToArray();
    }

    private static IReadOnlyList<string> EnumerateResolverFiles(IReadOnlyList<string> directories)
    {
        var files = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var dll in Directory.EnumerateFiles(dir, "*.dll", SearchOption.TopDirectoryOnly))
            {
                files.Add(dll);
            }
        }

        return files.ToArray();
    }
}
