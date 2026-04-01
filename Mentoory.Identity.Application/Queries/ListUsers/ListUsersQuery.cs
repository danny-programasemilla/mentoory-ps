using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Identity.Application.Queries.ListUsers;

/// <summary>
/// Query to retrieve a paginated list of users for DataTable display.
/// </summary>
/// <param name="Request">The DataTable request parameters including paging, sorting, and filtering.</param>
/// <param name="IncubatorId">Optional incubator ID to filter users with active role assignments.</param>
public sealed record ListUsersQuery(DataTableRequest Request, long? IncubatorId = null) : IBaseRequest<DataTableResponse<UserListItemDto>>;
