using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Mentoory.Example.Application;

/// <summary>
/// Provides extension methods for configuring Example application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Example application services to the dependency injection container.
    /// Registers MediatR handlers and FluentValidation validators from the Example.Application assembly.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExampleApplication(this IServiceCollection services)
    {
        // Register MediatR services from the executing assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
        });

        // Register FluentValidation validators from the executing assembly
        FluentValidation.AssemblyScanner
            .FindValidatorsInAssembly(Assembly.GetExecutingAssembly())
            .ForEach(item => services.AddScoped(item.InterfaceType, item.ValidatorType));

        return services;
    }
}
