using Mentoory.Example.Domain.Repositories;
using Mentoory.Example.Infrastructure.Persistence;
using Mentoory.Example.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mentoory.Example.Infrastructure;

/// <summary>
/// Provides extension methods for setting up Business Incubator infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Business Incubator infrastructure services to the specified <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to add services to.</param>
    /// <param name="connectionName">The name of the connection string to use. Defaults to "DefaultConnection".</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection string is not found.</exception>
    public static IHostApplicationBuilder AddExampleInfrastructure(this IHostApplicationBuilder builder, string connectionName = "DefaultConnection")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName) ?? throw new InvalidOperationException($"Connection string '{connectionName}' not found.");

        builder.Services.AddDbContext<ExampleDbContext>(opts =>
        {
            opts.UseSqlServer(connectionString);
            opts.EnableSensitiveDataLogging();
            opts.EnableDetailedErrors();
        });

        // Aspire extension
        builder.EnrichSqlServerDbContext<ExampleDbContext>(settings =>
        {
            settings.CommandTimeout = 30;
        });

        builder.Services.AddScoped<IExampleRepository, ExampleRepository>();

        return builder;
    }
}
