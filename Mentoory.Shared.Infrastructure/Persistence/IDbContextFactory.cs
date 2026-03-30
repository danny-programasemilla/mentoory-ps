using System.Diagnostics.CodeAnalysis;

namespace Mentoory.Shared.Infrastructure.Persistence;

/// <summary>
/// Factory interface for retrieving database contexts based on request types.
/// </summary>
public interface IDbContextFactory
{
    /// <summary>
    /// Attempts to retrieve the appropriate database context for the specified request type.
    /// </summary>
    /// <typeparam name="TRequest">The type of request for which to retrieve the database context.</typeparam>
    /// <param name="dbContext">When this method returns, contains the database context if found; otherwise, null.</param>
    /// <returns><c>true</c> if a database context was found for the request type; otherwise, <c>false</c>.</returns>
    bool TryGetDbContextForRequest<TRequest>([NotNullWhen(true)] out IDbContext? dbContext);
}
