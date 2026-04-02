using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Authorization.Application.Queries.ListIncubatorMembers;

/// <summary>
/// Query to retrieve a paginated list of users who are members of a specific incubator.
/// </summary>
/// <param name="Request">The DataTable request parameters including paging, sorting, and filtering.</param>
/// <param name="IncubatorId">The incubator ID to filter members by.</param>
public sealed record ListIncubatorMembersQuery(
    DataTableRequest Request,
    long IncubatorId) : IBaseRequest<DataTableResponse<IncubatorMemberListItemDto>>;
