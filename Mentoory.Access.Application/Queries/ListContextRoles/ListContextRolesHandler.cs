using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Domain.Constants;

namespace Mentoory.Access.Application.Queries.ListContextRoles;

public class ListContextRolesHandler(IRoleAssignmentRepository repository)
    : BaseCommandHandler<ListContextRolesQuery, List<ContextRoleDto>>
{
    private static readonly Dictionary<string, string> RoleDisplayNames = new()
    {
        [Roles.GlobalAdmin] = "Administrador Global",
        [Roles.IncubatorAdmin] = "Administrador de Incubadora",
        [Roles.ProjectCoordinator] = "Coordinador de Proyecto",
        [Roles.Mentor] = "Mentor",
        [Roles.Entrepreneur] = "Emprendedor",
        [Roles.Sponsor] = "Patrocinador",
    };

    private static readonly Dictionary<string, int> RoleOrder = Roles.All
        .Select((role, index) => (role, index))
        .ToDictionary(x => x.role, x => x.index);

    public override async Task<Result<List<ContextRoleDto>>> Handle(
        ListContextRolesQuery request,
        CancellationToken cancellationToken)
    {
        var assignments = await repository.GetActiveByUserIdAsync(
            request.UserId, cancellationToken);

        var roles = assignments
            .Select(a => a.Role)
            .Distinct()
            .OrderBy(r => RoleOrder.GetValueOrDefault(r, int.MaxValue))
            .Select(r => new ContextRoleDto(
                r,
                RoleDisplayNames.GetValueOrDefault(r, r)))
            .ToList();

        return Success(roles);
    }
}
