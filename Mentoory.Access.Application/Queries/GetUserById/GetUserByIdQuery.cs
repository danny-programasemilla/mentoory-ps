using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetUserById;

public sealed record GetUserByIdQuery(long UserId) : IBaseRequest<User?>;
