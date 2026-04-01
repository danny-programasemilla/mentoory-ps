using System.Linq.Expressions;
using LinaSys.Shared.Application.Extensions;
using Mentoory.Identity.Application.Queries.ListUsers.Abstractions;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Identity.Application.Queries.ListUsers;

/// <summary>
/// Handles the ListUsersQuery by querying the database for a paginated list of users.
/// </summary>
public class ListUsersHandler : BaseCommandHandler<ListUsersQuery, DataTableResponse<UserListItemDto>>
{
    private static readonly Dictionary<string, Expression<Func<UserListItemDto, object?>>> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        { "email", x => x.Email },
        { "firstName", x => x.FirstName },
        { "lastName", x => x.LastName },
        { "accountStatus", x => x.AccountStatus },
        { "createdAtUtc", x => x.CreatedAtUtc },
    };

    private readonly IIdentityQueryContext _queryContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListUsersHandler"/> class.
    /// </summary>
    /// <param name="queryContext">The identity query context for read-only data access.</param>
    public ListUsersHandler(IIdentityQueryContext queryContext)
    {
        _queryContext = queryContext;
    }

    /// <inheritdoc />
    public override async Task<Result<DataTableResponse<UserListItemDto>>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var dataTableRequest = request.Request;

        var usersQuery = _queryContext.UsersQueryable();

        if (request.IncubatorId.HasValue)
        {
            var incubatorUserIds = _queryContext.ActiveUserIdsByIncubatorQueryable(request.IncubatorId.Value);
            usersQuery = usersQuery.Where(u => incubatorUserIds.Contains(u.Id));
        }

        var query = usersQuery
            .Select(u => new UserListItemDto(
                u.ExternalId,
                u.Email.Value,
                u.FirstName,
                u.LastName,
                u.AccountStatus.ToString(),
                u.CreatedAtUtc));

        var totalRecords = await query.CountAsync(cancellationToken);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(dataTableRequest.SearchValue))
        {
            var search = dataTableRequest.SearchValue.ToUpperInvariant();
            query = query.Where(u =>
                u.Email.ToUpper().Contains(search) ||
                u.FirstName.ToUpper().Contains(search) ||
                u.LastName.ToUpper().Contains(search));
        }

        var filteredRecords = await query.CountAsync(cancellationToken);

        // Apply sorting and paging
        query = query
            .ApplyOrdering(dataTableRequest.SortColumn, dataTableRequest.SortDirection, SortColumns)
            .ApplyPaging(dataTableRequest.Start, dataTableRequest.Length);

        var data = await query.ToListAsync(cancellationToken);

        var response = new DataTableResponse<UserListItemDto>(
            dataTableRequest.Draw,
            totalRecords,
            filteredRecords,
            data);

        return Success(response);
    }
}
