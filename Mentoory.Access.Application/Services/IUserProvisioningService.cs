namespace Mentoory.Access.Application.Services;

public interface IUserProvisioningService
{
    Task<UserProvisioningOutcome> ProvisionAsync(
        UserProvisioningRequest request,
        CancellationToken cancellationToken);
}
