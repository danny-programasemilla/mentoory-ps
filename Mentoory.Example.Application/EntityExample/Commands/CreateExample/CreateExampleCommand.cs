using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Example.Application.EntityExample.Commands.CreateExample;

/// <summary>
/// Represents a command to create a new example entity with a title and creation date.
/// Implements the CQRS pattern through MediatR and returns the created Example aggregate root upon successful execution.
/// </summary>
/// <param name="Title">The title of the example entity to be created.</param>
/// <param name="DateCreated">The creation timestamp for the example entity.</param>
public record CreateExampleCommand(string Title, DateTime DateCreated) : IBaseRequest<Domain.Aggregates.Example.Example>;
