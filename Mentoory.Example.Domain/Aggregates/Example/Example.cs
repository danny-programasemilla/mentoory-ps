using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Example.Domain.Aggregates.Example;

/// <summary>
/// Represents an example aggregate root entity in the domain model.
/// </summary>
public class Example : Entity, IAggregateRoot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Example"/> class.
    /// </summary>
    /// <param name="title">The title of the example. Cannot be null or whitespace.</param>
    /// <param name="dateCreated">The date when the example was created.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="title"/> is null or whitespace.</exception>
    public Example(string title, DateTime dateCreated)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentNullException(nameof(title));
        }

        DateCreated = dateCreated;
        Title = title;
    }

    /// <summary>
    /// Gets or sets the date when the example was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Gets or sets the title of the example.
    /// </summary>
    public string Title { get; set; }
}
