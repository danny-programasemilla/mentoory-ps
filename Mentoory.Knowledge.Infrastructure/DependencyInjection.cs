using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Knowledge.Infrastructure.Persistence;
using Mentoory.Knowledge.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mentoory.Knowledge.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddKnowledgeInfrastructure(
        this IHostApplicationBuilder builder,
        string connectionName = "DefaultConnection")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"Connection string '{connectionName}' not found.");

        builder.Services.AddDbContext<KnowledgeDbContext>(opts =>
        {
            opts.UseSqlServer(connectionString);
            opts.EnableSensitiveDataLogging();
            opts.EnableDetailedErrors();
        });

        builder.EnrichSqlServerDbContext<KnowledgeDbContext>(settings =>
        {
            settings.CommandTimeout = 30;
        });

        builder.Services.AddScoped<IKnowledgeStructureTemplateRepository, KnowledgeStructureTemplateRepository>();
        builder.Services.AddScoped<IKnowledgeStructureRepository, KnowledgeStructureRepository>();

        return builder;
    }
}
