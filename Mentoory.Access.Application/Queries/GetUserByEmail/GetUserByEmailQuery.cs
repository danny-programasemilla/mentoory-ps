using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetUserByEmail;

/// <summary>
/// Query to retrieve a user by their email address.
/// </summary>
/// <param name="NormalizedEmail">The normalized (uppercase, trimmed) email address to search for.</param>
public sealed record GetUserByEmailQuery(string NormalizedEmail) : IBaseRequest<User?>;
