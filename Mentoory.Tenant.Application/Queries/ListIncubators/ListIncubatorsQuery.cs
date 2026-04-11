using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.ListIncubators;

public sealed record ListIncubatorsQuery(DataTableRequest DataTableRequest)
    : IBaseRequest<DataTableResponse<IncubatorDto>>;
