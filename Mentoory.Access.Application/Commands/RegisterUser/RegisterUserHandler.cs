using Mentoory.Access.Application.Services;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.RegisterUser;

public partial class RegisterUserHandler : BaseCommandHandler<RegisterUserCommand>
{
    private readonly IUserRegistrationService _registrationService;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        IUserRegistrationService registrationService,
        ILogger<RegisterUserHandler> logger)
    {
        _registrationService = registrationService;
        _logger = logger;
    }

    public override async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var registrationRequest = new UserRegistrationRequest(
            request.Email,
            request.Country,
            request.NationalId,
            request.FirstName,
            request.LastName,
            request.Password,
            ProjectExternalId: null,
            EmailVerificationMode.Required,
            EnrollmentVariant: EnrollmentVariants.SelfRegistration,
            RequirePasswordReset: false);

        var result = await _registrationService.RegisterAsync(registrationRequest, cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.ErrorCode!.Value, result.ErrorMessages!);
        }

        LogUserRegistered(result.Value!.Email);

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User registered successfully. Email: {Email}")]
    partial void LogUserRegistered(string email);
}
