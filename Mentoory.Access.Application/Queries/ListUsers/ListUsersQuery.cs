using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.ListUsers;

/// <summary>
/// Query to retrieve a paginated list of all users for DataTable display.
/// Used by GlobalAdmin (Platform area) to list all users in the system.
/// For incubator-scoped user lists, use ListIncubatorMembersQuery in the Authorization domain.
/// </summary>
/// <param name="Request">The DataTable request parameters including paging, sorting, and filtering.</param>
public sealed record ListUsersQuery(DataTableRequest Request) : IBaseRequest<DataTableResponse<UserListItemDto>>;
