using System.Reflection;
using Mentoory.Access.Application.Infrastructure;
using Mentoory.Access.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mentoory.Access.Application;

/// <summary>
/// Provides extension methods for configuring Access application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Access application services to the dependency injection container.
    /// Registers MediatR handlers and FluentValidation validators from the Access.Application assembly.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddAccessApplication(this IServiceCollection services)
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

        services.AddScoped<IUserProvisioningService, UserProvisioningService>();

        return services;
    }
}
