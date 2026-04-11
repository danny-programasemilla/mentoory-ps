using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Configuration.Commands.UpdateConfiguration;

/// <summary>
/// Command to update a system configuration value.
/// </summary>
/// <param name="Key">The configuration key to update.</param>
/// <param name="Value">The new value for the configuration key.</param>
public sealed record UpdateConfigurationCommand(string Key, string Value) : IBaseRequest;
