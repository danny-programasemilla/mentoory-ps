using System.Collections.Concurrent;

namespace LinaSys.Shared.Application.Extensions;

/// <summary>
/// Extension methods for generic type names.
/// </summary>
public static class GenericTypeExtensions
{
    private static readonly ConcurrentDictionary<Type, string> _typeNameCache = new();

    /// <summary>
    /// Gets the generic type name of the specified type.
    /// </summary>
    /// <param name="type">The type to get the generic type name for.</param>
    /// <returns>The generic type name.</returns>
    public static string GetGenericTypeName(this Type type)
    {
        return _typeNameCache.GetOrAdd(type, t =>
        {
            if (!t.IsGenericType)
            {
                return t.Name;
            }

            var genericTypes = string.Join(",", t.GetGenericArguments().Select(arg => arg.Name));
            return $"{t.Name[..t.Name.IndexOf('`')]}<{genericTypes}>";
        });
    }

    /// <summary>
    /// Gets the generic type name of the specified object.
    /// </summary>
    /// <param name="object">The object to get the generic type name for.</param>
    /// <returns>The generic type name.</returns>
    public static string GetGenericTypeName(this object @object)
    {
        return @object.GetType().GetGenericTypeName();
    }
}
