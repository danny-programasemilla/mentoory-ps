using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.AssignRole;

/// <summary>
/// Command to assign a role to a user within an incubator and optionally a project.
/// </summary>
/// <param name="UserId">The identifier of the user to assign the role to.</param>
/// <param name="IncubatorId">The identifier of the incubator context for the role.</param>
/// <param name="ProjectId">The optional project identifier for project-scoped roles.</param>
/// <param name="Role">The role name to assign (must be a valid platform role).</param>
public sealed record AssignRoleCommand(
    long UserId,
    long IncubatorId,
    long? ProjectId,
    string Role) : IBaseRequest;
