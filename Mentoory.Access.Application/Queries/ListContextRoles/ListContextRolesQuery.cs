using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.ListContextRoles;

public sealed record ListContextRolesQuery(long UserId) : IBaseRequest<List<ContextRoleDto>>;
