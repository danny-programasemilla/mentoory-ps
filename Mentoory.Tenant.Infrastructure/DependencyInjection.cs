using Mentoory.Tenant.Domain.Repositories;
using Mentoory.Tenant.Infrastructure.Persistence;
using Mentoory.Tenant.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mentoory.Tenant.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddTenantInfrastructure(
        this IHostApplicationBuilder builder,
        string connectionName = "DefaultConnection")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"Connection string '{connectionName}' not found.");

        builder.Services.AddDbContext<TenantDbContext>(opts =>
        {
            opts.UseSqlServer(connectionString);
            opts.EnableSensitiveDataLogging();
            opts.EnableDetailedErrors();
        });

        builder.EnrichSqlServerDbContext<TenantDbContext>(settings =>
        {
            settings.CommandTimeout = 30;
        });

        builder.Services.AddScoped<IIncubatorRepository, IncubatorRepository>();
        builder.Services.AddScoped<IProjectRepository, ProjectRepository>();

        return builder;
    }
}
