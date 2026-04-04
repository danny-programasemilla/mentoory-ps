using System.Linq.Expressions;
using LinaSys.Shared.Application.Extensions;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Access.Application.Queries.ListIncubatorMembers;

/// <summary>
/// Handles the ListIncubatorMembersQuery by querying the Access domain's
/// UserProfile read model filtered by active role assignments for an incubator.
/// </summary>
public class ListIncubatorMembersHandler(
    IUserProfileRepository userProfileRepository,
    IRoleAssignmentRepository roleAssignmentRepository)
    : BaseCommandHandler<ListIncubatorMembersQuery, DataTableResponse<IncubatorMemberListItemDto>>
{
    private static readonly Dictionary<string, Expression<Func<IncubatorMemberListItemDto, object?>>> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        { "email", x => x.Email },
        { "firstName", x => x.FirstName },
        { "lastName", x => x.LastName },
        { "accountStatus", x => x.AccountStatus },
        { "createdAtUtc", x => x.CreatedAtUtc },
    };

    /// <inheritdoc />
    public override async Task<Result<DataTableResponse<IncubatorMemberListItemDto>>> Handle(
        ListIncubatorMembersQuery request,
        CancellationToken cancellationToken)
    {
        var dataTableRequest = request.Request;

        var memberUserIds = roleAssignmentRepository.Query()
            .Where(ra => ra.IncubatorId == request.IncubatorId && ra.IsActive)
            .Select(ra => ra.UserId)
            .Distinct();

        var query = userProfileRepository.Query()
            .Where(u => memberUserIds.Contains(u.UserId))
            .Select(u => new IncubatorMemberListItemDto(
                u.UserExternalId,
                u.Email,
                u.FirstName,
                u.LastName,
                u.AccountStatus,
                u.CreatedAtUtc));

        var totalRecords = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(dataTableRequest.SearchValue))
        {
            var search = dataTableRequest.SearchValue.ToUpperInvariant();
            query = query.Where(u =>
                u.Email.ToUpper().Contains(search) ||
                u.FirstName.ToUpper().Contains(search) ||
                u.LastName.ToUpper().Contains(search));
        }

        var filteredRecords = await query.CountAsync(cancellationToken);

        query = query
            .ApplyOrdering(dataTableRequest.SortColumn, dataTableRequest.SortDirection, SortColumns)
            .ApplyPaging(dataTableRequest.Start, dataTableRequest.Length);

        var data = await query.ToListAsync(cancellationToken);

        var response = new DataTableResponse<IncubatorMemberListItemDto>(
            dataTableRequest.Draw,
            totalRecords,
            filteredRecords,
            data);

        return Success(response);
    }
}
