using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;

namespace Mentoory.Access.Application.Configuration.Commands.UpdateConfiguration;

/// <summary>
/// Handles updating a system configuration value.
/// </summary>
public class UpdateConfigurationHandler : BaseCommandHandler<UpdateConfigurationCommand>
{
    private readonly ISystemConfigurationRepository _repository;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateConfigurationHandler"/> class.
    /// </summary>
    /// <param name="repository">The system configuration repository for persistence operations.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    public UpdateConfigurationHandler(
        ISystemConfigurationRepository repository,
        ITimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(UpdateConfigurationCommand request, CancellationToken cancellationToken)
    {
        var config = await _repository.GetByKeyAsync(request.Key, cancellationToken);

        if (config is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("UpdateConfiguration", $"Clave de configuración '{request.Key}' no encontrada."));
        }

        var validationError = ValidateValueForDataType(config.DataType, request.Value);
        if (validationError is not null)
        {
            return Failure(ResultErrorCodes.Validation_SomeFieldsAreInvalid, ("UpdateConfiguration", validationError));
        }

        config.Update(request.Value, _timeProvider.UtcNow);

        _repository.Update(config);
        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Success();
    }

    private static string? ValidateValueForDataType(string dataType, string value)
    {
        return dataType switch
        {
            "Integer" => ValidateInteger(value),
            "Boolean" => ValidateBoolean(value),
            _ => null
        };
    }

    private static string? ValidateInteger(string value)
    {
        if (!int.TryParse(value, out var intValue))
        {
            return "El valor debe ser un número entero válido.";
        }

        if (intValue <= 0)
        {
            return "El valor numérico debe ser mayor a cero.";
        }

        return null;
    }

    private static string? ValidateBoolean(string value)
    {
        if (!bool.TryParse(value, out _))
        {
            return "El valor debe ser 'true' o 'false'.";
        }

        return null;
    }
}
