using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Mentoory.Knowledge.Application;

/// <summary>
/// Provides extension methods for configuring Knowledge application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Knowledge application services to the dependency injection container.
    /// Registers MediatR handlers and FluentValidation validators from the Knowledge.Application assembly.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddKnowledgeApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
        });

        FluentValidation.AssemblyScanner
            .FindValidatorsInAssembly(Assembly.GetExecutingAssembly())
            .ForEach(item => services.AddScoped(item.InterfaceType, item.ValidatorType));

        return services;
    }
}
