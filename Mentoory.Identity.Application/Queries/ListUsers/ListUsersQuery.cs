using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Identity.Application.Queries.ListUsers;

/// <summary>
/// Query to retrieve a paginated list of users for DataTable display.
/// </summary>
/// <param name="Request">The DataTable request parameters including paging, sorting, and filtering.</param>
public sealed record ListUsersQuery(DataTableRequest Request) : IBaseRequest<DataTableResponse<UserListItemDto>>;
