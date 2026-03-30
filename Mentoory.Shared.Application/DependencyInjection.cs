using Mentoory.Shared.Application.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Mentoory.Shared.Application;

/// <summary>
/// Extension methods for registering shared application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers shared application services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSharedApplication(this IServiceCollection services)
    {
        // Register integration event service
        services.AddScoped<IIntegrationEventService, MediatRIntegrationEventService>();

        return services;
    }
}
