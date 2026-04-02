using Mentoory.Authorization.Domain.Repositories;
using Mentoory.Authorization.Infrastructure.Persistence;
using Mentoory.Authorization.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mentoory.Authorization.Infrastructure;

/// <summary>
/// Provides extension methods for setting up Authorization infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Authorization infrastructure services to the specified <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to add services to.</param>
    /// <param name="connectionName">The name of the connection string to use. Defaults to "DefaultConnection".</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection string is not found.</exception>
    public static IHostApplicationBuilder AddAuthorizationInfrastructure(
        this IHostApplicationBuilder builder,
        string connectionName = "DefaultConnection")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"Connection string '{connectionName}' not found.");

        builder.Services.AddDbContext<AuthorizationDbContext>(opts =>
        {
            opts.UseSqlServer(connectionString);
            opts.EnableSensitiveDataLogging();
            opts.EnableDetailedErrors();
        });

        // Aspire extension
        builder.EnrichSqlServerDbContext<AuthorizationDbContext>(settings =>
        {
            settings.CommandTimeout = 30;
        });

        builder.Services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
        builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        return builder;
    }
}
