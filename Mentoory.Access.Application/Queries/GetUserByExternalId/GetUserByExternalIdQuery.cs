using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetUserByExternalId;

/// <summary>
/// Query to retrieve a user by their external identifier.
/// </summary>
/// <param name="ExternalId">The external GUID identifier of the user.</param>
public sealed record GetUserByExternalIdQuery(Guid ExternalId) : IBaseRequest<User?>;
