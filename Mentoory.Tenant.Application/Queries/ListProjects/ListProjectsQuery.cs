using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.ListProjects;

public sealed record ListProjectsQuery(DataTableRequest DataTableRequest, long? IncubatorId = null)
    : IBaseRequest<DataTableResponse<ProjectDto>>;
