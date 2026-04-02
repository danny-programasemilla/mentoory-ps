using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Mentoory.Authorization.Tests.Infrastructure;

/// <summary>
/// Wraps an in-memory collection as an IQueryable that supports EF Core async operations.
/// Used in unit tests where handlers call CountAsync, ToListAsync, etc.
/// </summary>
internal static class TestAsyncQueryable
{
    public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source) =>
        new AsyncQueryableWrapper<T>(source.AsQueryable());

    private sealed class AsyncQueryableWrapper<T>(IQueryable<T> inner)
        : IOrderedQueryable<T>, IAsyncEnumerable<T>, IQueryProvider, IAsyncQueryProvider
    {
        public Type ElementType => inner.ElementType;
        public Expression Expression => inner.Expression;
        public IQueryProvider Provider => this;

        public IQueryable CreateQuery(Expression expression) =>
            new AsyncQueryableWrapper<T>((IQueryable<T>)inner.Provider.CreateQuery(expression));

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new AsyncQueryableWrapper<TElement>(inner.Provider.CreateQuery<TElement>(expression));

        public object? Execute(Expression expression) =>
            inner.Provider.Execute(expression);

        public TResult Execute<TResult>(Expression expression) =>
            inner.Provider.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var innerType = resultType.GetGenericArguments()[0];
                var executeMethod = typeof(IQueryProvider).GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!;
                var result = executeMethod.MakeGenericMethod(innerType).Invoke(inner.Provider, [expression]);

                var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(innerType);
                return (TResult)fromResultMethod.Invoke(null, [result])!;
            }

            return inner.Provider.Execute<TResult>(expression);
        }

        public IEnumerator<T> GetEnumerator() => inner.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => inner.GetEnumerator();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new AsyncEnumerator<T>(inner.GetEnumerator());
    }

    private sealed class AsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;
        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
