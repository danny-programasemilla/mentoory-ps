using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.ListProjectForms;

/// <summary>
/// Query to retrieve a paginated list of project forms for a given project.
/// </summary>
public sealed record ListProjectFormsQuery(
    DataTableRequest Request,
    long ProjectId) : IBaseRequest<DataTableResponse<ProjectFormListItemDto>>;
