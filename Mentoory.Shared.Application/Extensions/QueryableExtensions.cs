using System.Linq.Expressions;

namespace LinaSys.Shared.Application.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IQueryable{T}"/> to apply ordering and paging.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies ordering to the query based on the specified column and direction.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the query.</typeparam>
    /// <param name="query">The query to apply ordering to.</param>
    /// <param name="orderBy">The column to order by.</param>
    /// <param name="direction">The direction of ordering (asc/desc).</param>
    /// <param name="columns">A dictionary mapping column names to their corresponding key selectors.</param>
    /// <returns>The ordered query.</returns>
    public static IQueryable<T> ApplyOrdering<T>(
        this IQueryable<T> query,
        string? orderBy,
        string? direction,
        Dictionary<string, Expression<Func<T, object?>>> columns)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return query;
        }

        direction = direction?.ToLowerInvariant() ?? "asc";

        if (!columns.TryGetValue(orderBy.ToLowerInvariant(), out var keySelector))
        {
            return query;
        }

        return direction == "desc"
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    /// <summary>
    /// Applies paging to the query based on the specified skip and take values.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the query.</typeparam>
    /// <param name="query">The query to apply paging to.</param>
    /// <param name="skip">The number of elements to skip.</param>
    /// <param name="take">The number of elements to take.</param>
    /// <returns>The paged query.</returns>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int skip, int take)
        => query.Skip(skip).Take(take);
}
