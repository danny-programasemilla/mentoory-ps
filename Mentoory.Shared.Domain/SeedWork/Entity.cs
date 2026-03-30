using MediatR;

namespace Mentoory.Shared.Domain.SeedWork;

/// <summary>
/// Base class for entities in the domain model.
/// </summary>
public abstract class Entity
{
    private readonly List<INotification> _domainEvents = [];

    private int? _requestedHashCode;

    /// <summary>
    /// Gets the domain events associated with the entity.
    /// </summary>
    public IReadOnlyCollection<INotification>? DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public virtual long Id { get; protected set; }

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="left">The left entity.</param>
    /// <param name="right">The right entity.</param>
    /// <returns>True if the entities are not equal, otherwise false.</returns>
    public static bool operator !=(Entity left, Entity right) => !(left == right);

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    /// <param name="left">The left entity.</param>
    /// <param name="right">The right entity.</param>
    /// <returns>True if the entities are equal, otherwise false.</returns>
    public static bool operator ==(Entity left, Entity right) => left?.Equals(right) ?? Equals(right, null);

    /// <summary>
    /// Adds a domain event to the entity.
    /// </summary>
    /// <param name="eventItem">The domain event to add.</param>
    public void AddDomainEvent(INotification eventItem) => _domainEvents.Add(eventItem);

    /// <summary>
    /// Clears all domain events from the entity.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents?.Clear();

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>True if the specified object is equal to the current entity, otherwise false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity item)
        {
            return false;
        }

        if (ReferenceEquals(this, item))
        {
            return true;
        }

        if (GetType() != item.GetType())
        {
            return false;
        }

        if (item.IsTransient() || IsTransient())
        {
            return false;
        }

        return item.Id == Id;
    }

    /// <summary>
    /// Returns the hash code for the current entity.
    /// </summary>
    /// <returns>The hash code for the current entity.</returns>
    public override int GetHashCode()
    {
        if (IsTransient())
        {
            return base.GetHashCode();
        }

        _requestedHashCode ??= Id.GetHashCode() ^ 31;

        return _requestedHashCode.Value;
    }

    /// <summary>
    /// Determines whether the entity is transient (not yet persisted).
    /// </summary>
    /// <returns>True if the entity is transient, otherwise false.</returns>
    public bool IsTransient() => Id == 0;

    /// <summary>
    /// Removes a domain event from the entity.
    /// </summary>
    /// <param name="eventItem">The domain event to remove.</param>
    public void RemoveDomainEvent(INotification eventItem) => _domainEvents?.Remove(eventItem);
}
