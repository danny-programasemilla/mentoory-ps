using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Configuration.Queries.GetConfiguration;

/// <summary>
/// Query to retrieve a system configuration value by its key.
/// </summary>
/// <param name="Key">The configuration key to look up.</param>
public sealed record GetConfigurationQuery(string Key) : IBaseRequest<string>;
