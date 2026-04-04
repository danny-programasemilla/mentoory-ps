using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.RevokeRole;

/// <summary>
/// Command to revoke an existing role assignment by its external identifier.
/// </summary>
/// <param name="RoleAssignmentExternalId">The external identifier of the role assignment to revoke.</param>
public sealed record RevokeRoleCommand(Guid RoleAssignmentExternalId) : IBaseRequest;
