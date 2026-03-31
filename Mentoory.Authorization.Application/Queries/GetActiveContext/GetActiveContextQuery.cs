using Mentoory.Authorization.Domain.ReadModels;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Authorization.Application.Queries.GetActiveContext;

/// <summary>
/// Query to retrieve a single active role assignment context by its external identifier.
/// </summary>
/// <param name="UserId">The identifier of the user owning the context.</param>
/// <param name="RoleAssignmentExternalId">The external identifier of the role assignment.</param>
public sealed record GetActiveContextQuery(
    long UserId,
    Guid RoleAssignmentExternalId) : IBaseRequest<UserContext>;
