using Mentoory.Access.Application.Configuration;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Access.Infrastructure.Persistence.Repositories;
using Mentoory.Access.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mentoory.Access.Infrastructure;

/// <summary>
/// Provides extension methods for setting up Access infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Access infrastructure services to the specified <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to add services to.</param>
    /// <param name="connectionName">The name of the connection string to use. Defaults to "DefaultConnection".</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection string is not found.</exception>
    public static IHostApplicationBuilder AddAccessInfrastructure(
        this IHostApplicationBuilder builder,
        string connectionName = "DefaultConnection")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"Connection string '{connectionName}' not found.");

        builder.Services.AddDbContext<AccessDbContext>(opts =>
        {
            opts.UseSqlServer(connectionString);
            opts.EnableSensitiveDataLogging();
            opts.EnableDetailedErrors();
        });

        builder.EnrichSqlServerDbContext<AccessDbContext>(settings =>
        {
            settings.CommandTimeout = 30;
        });

        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
        builder.Services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
        builder.Services.AddScoped<ISystemConfigurationRepository, SystemConfigurationRepository>();
        builder.Services.AddScoped<ICountryRepository, CountryRepository>();
        builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        builder.Services.AddScoped<ISystemConfigurationReader, SystemConfigurationReader>();

        return builder;
    }
}
