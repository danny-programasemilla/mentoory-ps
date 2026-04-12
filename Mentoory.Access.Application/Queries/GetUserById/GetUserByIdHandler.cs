using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetUserById;

public class GetUserByIdHandler(IUserRepository userRepository)
    : BaseCommandHandler<GetUserByIdQuery, User?>
{
    public override async Task<Result<User?>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        return Success(user);
    }
}
