using Mentoory.Access.Domain.ReadModels;
using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.SetActiveContext;

/// <summary>
/// Command to set the active context for a user by selecting a role assignment.
/// Returns the UserContext read model so the web layer can update the session.
/// </summary>
/// <param name="UserId">The identifier of the user setting the context.</param>
/// <param name="RoleAssignmentExternalId">The external identifier of the role assignment to activate.</param>
[Audited(AuditEventTypes.ContextActivated, EntityType = "TenantContext")]
public sealed record SetActiveContextCommand(
    long UserId,
    Guid RoleAssignmentExternalId) : IBaseRequest<UserContext>;
