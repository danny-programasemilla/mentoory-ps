using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.GetDashboardMetrics;

public sealed record GetDashboardMetricsQuery(long IncubatorId) : IBaseRequest<DashboardMetricsDto>;
