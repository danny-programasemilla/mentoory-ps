using Mentoory.Authorization.Domain.ReadModels;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Authorization.Application.Queries.GetUserContexts;

/// <summary>
/// Query to retrieve all active role assignment contexts for a user.
/// </summary>
/// <param name="UserId">The identifier of the user whose contexts to retrieve.</param>
public sealed record GetUserContextsQuery(long UserId) : IBaseRequest<List<UserContext>>;
