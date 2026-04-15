using Mentoory.Shared.Application;

namespace Mentoory.Access.Application.Services;

public interface IUserRegistrationService
{
    Task<Result<UserRegistrationResult>> RegisterAsync(UserRegistrationRequest request, CancellationToken cancellationToken);
}
