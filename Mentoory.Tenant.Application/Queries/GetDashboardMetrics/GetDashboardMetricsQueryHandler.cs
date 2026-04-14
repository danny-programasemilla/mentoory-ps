using MediatR;
using Mentoory.Access.Application.Queries.GetActiveUserCount;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Repositories;

namespace Mentoory.Tenant.Application.Queries.GetDashboardMetrics;

public sealed class GetDashboardMetricsQueryHandler(
    ISender sender,
    IProjectRepository projectRepository)
    : BaseCommandHandler<GetDashboardMetricsQuery, DashboardMetricsDto>
{
    public override async Task<Result<DashboardMetricsDto>> Handle(
        GetDashboardMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var userCountResult = await sender.Send(new GetActiveUserCountQuery(request.IncubatorId), cancellationToken);
        var userCount = userCountResult.IsSuccess ? userCountResult.Value : 0;

        var projectCount = await projectRepository.CountAsync(
            projectRepository.Query().Where(p => p.IncubatorId == request.IncubatorId),
            cancellationToken);

        return Success(new DashboardMetricsDto(userCount, projectCount));
    }
}
