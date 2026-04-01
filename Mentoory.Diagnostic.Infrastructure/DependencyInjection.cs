using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Diagnostic.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mentoory.Diagnostic.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddDiagnosticInfrastructure(
        this IHostApplicationBuilder builder,
        string connectionName = "DefaultConnection")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"Connection string '{connectionName}' not found.");

        builder.Services.AddDbContext<DiagnosticDbContext>(opts =>
        {
            opts.UseSqlServer(connectionString);
            opts.EnableSensitiveDataLogging();
            opts.EnableDetailedErrors();
        });

        builder.EnrichSqlServerDbContext<DiagnosticDbContext>(settings =>
        {
            settings.CommandTimeout = 30;
        });

        builder.Services.AddScoped<IFormTemplateRepository, FormTemplateRepository>();
        builder.Services.AddScoped<IProjectFormRepository, ProjectFormRepository>();
        builder.Services.AddScoped<IDiagnosticResponseRepository, DiagnosticResponseRepository>();

        return builder;
    }
}
