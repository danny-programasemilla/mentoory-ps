using Mentoory.Identity.Domain.Repositories;
using Mentoory.Identity.Domain.Services;
using Mentoory.Identity.Infrastructure.Persistence;
using Mentoory.Identity.Infrastructure.Persistence.Repositories;
using Mentoory.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mentoory.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddIdentityInfrastructure(
        this IHostApplicationBuilder builder,
        string connectionName = "DefaultConnection")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"Connection string '{connectionName}' not found.");

        builder.Services.AddDbContext<IdentityDbContext>(opts =>
        {
            opts.UseSqlServer(connectionString);
            opts.EnableSensitiveDataLogging();
            opts.EnableDetailedErrors();
        });

        builder.EnrichSqlServerDbContext<IdentityDbContext>(settings =>
        {
            settings.CommandTimeout = 30;
        });

        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
        builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        return builder;
    }
}
