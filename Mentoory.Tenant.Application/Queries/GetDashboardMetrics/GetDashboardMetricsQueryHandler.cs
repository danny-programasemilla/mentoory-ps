using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Tenant.Application.Queries.GetDashboardMetrics;

public class GetDashboardMetricsQueryHandler(
    IRoleAssignmentRepository roleAssignmentRepository,
    IProjectRepository projectRepository)
    : BaseCommandHandler<GetDashboardMetricsQuery, DashboardMetricsDto>
{
    public override async Task<Result<DashboardMetricsDto>> Handle(
        GetDashboardMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var userCount = await roleAssignmentRepository.Query()
            .Where(ra => ra.IncubatorId == request.IncubatorId && ra.IsActive)
            .Select(ra => ra.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var projectCount = await projectRepository.CountAsync(
            projectRepository.Query().Where(p => p.IncubatorId == request.IncubatorId),
            cancellationToken);

        return Success(new DashboardMetricsDto(userCount, projectCount));
    }
}
