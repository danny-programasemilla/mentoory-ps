using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetActiveUserCount;

public sealed record GetActiveUserCountQuery(long IncubatorId) : IBaseRequest<int>;
