using Mentoory.Access.Application.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.AdminEnrollUser;

public class AdminEnrollUserHandler : BaseCommandHandler<AdminEnrollUserCommand>
{
    private const string EmailField = "Email";
    private const string NationalIdField = "NationalId";

    private readonly IUserProvisioningService _provisioning;

    public AdminEnrollUserHandler(IUserProvisioningService provisioning)
    {
        _provisioning = provisioning;
    }

    public override async Task<Result> Handle(AdminEnrollUserCommand request, CancellationToken cancellationToken)
    {
        var provisioningRequest = new UserProvisioningRequest(
            request.Email,
            request.Country,
            request.NationalId,
            request.FirstName,
            request.LastName,
            request.Password);

        var outcome = await _provisioning.ProvisionAsync(provisioningRequest, cancellationToken);

        return outcome switch
        {
            UserProvisioningOutcome.Success => Success(),
            UserProvisioningOutcome.DuplicateEmail => Failure(
                ResultErrorCodes.GenericError,
                (EmailField, "Ya existe una cuenta con este correo electrónico.")),
            UserProvisioningOutcome.DuplicateNationalId => Failure(
                ResultErrorCodes.GenericError,
                (NationalIdField, "Ya existe una cuenta con este número de identificación.")),
            _ => throw new InvalidOperationException($"Unexpected provisioning outcome: {outcome}"),
        };
    }
}
