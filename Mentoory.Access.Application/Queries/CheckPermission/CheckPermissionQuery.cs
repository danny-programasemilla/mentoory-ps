using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.CheckPermission;

/// <summary>
/// Query to check if a user's active role has a specific permission.
/// </summary>
/// <param name="UserId">The identifier of the user to check.</param>
/// <param name="IncubatorId">The incubator context for the permission check.</param>
/// <param name="ProjectId">The optional project context for the permission check.</param>
/// <param name="Role">The role to check permissions for.</param>
/// <param name="Permission">The permission to verify.</param>
public sealed record CheckPermissionQuery(
    long UserId,
    long IncubatorId,
    long? ProjectId,
    string Role,
    string Permission) : IBaseRequest<bool>;
