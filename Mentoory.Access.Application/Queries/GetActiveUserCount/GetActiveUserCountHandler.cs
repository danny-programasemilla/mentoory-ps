using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Access.Application.Queries.GetActiveUserCount;

public sealed class GetActiveUserCountHandler(IRoleAssignmentRepository roleAssignmentRepository)
    : BaseCommandHandler<GetActiveUserCountQuery, int>
{
    public override async Task<Result<int>> Handle(
        GetActiveUserCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await roleAssignmentRepository.Query()
            .AsNoTracking()
            .Where(ra => ra.IncubatorId == request.IncubatorId && ra.IsActive)
            .Select(ra => ra.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        return Success(count);
    }
}
