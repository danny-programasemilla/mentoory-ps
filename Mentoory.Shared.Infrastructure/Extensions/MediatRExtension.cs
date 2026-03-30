using MediatR;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Shared.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for MediatR to support domain event dispatching.
/// </summary>
public static class MediatRExtension
{
    /// <summary>
    /// Dispatches all domain events from tracked entities in the specified database context.
    /// This method retrieves entities with pending domain events, clears them from the entities,
    /// and publishes each event through the MediatR mediator.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance used to publish domain events.</param>
    /// <param name="context">The Entity Framework Core database context containing tracked entities.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task DispatchDomainEventsAsync(this IMediator mediator, DbContext context)
    {
        var domainEntities = context.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents is not null && x.Entity.DomainEvents.Count != 0)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEntities.SelectMany(x => x.Entity.DomainEvents!))
        {
            await mediator.Publish(domainEvent);
        }
    }
}
