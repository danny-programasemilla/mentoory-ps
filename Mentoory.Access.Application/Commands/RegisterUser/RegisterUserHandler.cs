using Mentoory.Access.Application.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.RegisterUser;

public partial class RegisterUserHandler : BaseCommandHandler<RegisterUserCommand>
{
    private readonly IUserProvisioningService _provisioning;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        IUserProvisioningService provisioning,
        ILogger<RegisterUserHandler> logger)
    {
        _provisioning = provisioning;
        _logger = logger;
    }

    public override async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var provisioningRequest = new UserProvisioningRequest(
            request.Email,
            request.Country,
            request.NationalId,
            request.FirstName,
            request.LastName,
            request.Password);

        var outcome = await _provisioning.ProvisionAsync(provisioningRequest, cancellationToken);

        LogPublicRegistrationOutcome(
            request.Email,
            outcome,
            request.CorrelationId ?? string.Empty,
            request.ClientIpAddress ?? string.Empty);

        return Success();
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Public registration outcome. Email: {Email}, Outcome: {Outcome}, CorrelationId: {CorrelationId}, ClientIp: {ClientIp}")]
    partial void LogPublicRegistrationOutcome(string email, UserProvisioningOutcome outcome, string correlationId, string clientIp);
}
