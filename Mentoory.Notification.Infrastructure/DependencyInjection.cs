using Mentoory.Notification.Application.Services;
using Mentoory.Notification.Domain.Repositories;
using Mentoory.Notification.Contracts.Configuration;
using Mentoory.Notification.Infrastructure.Persistence;
using Mentoory.Notification.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mentoory.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddNotificationInfrastructure(
        this IHostApplicationBuilder builder,
        string connectionName = "DefaultConnection")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"Connection string '{connectionName}' not found.");

        builder.Services.AddDbContext<NotificationDbContext>(opts =>
        {
            opts.UseSqlServer(connectionString);
            opts.EnableSensitiveDataLogging();
            opts.EnableDetailedErrors();
        });

        builder.EnrichSqlServerDbContext<NotificationDbContext>(settings =>
        {
            settings.CommandTimeout = 30;
        });

        builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
        builder.Services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        builder.Services.AddScoped<INotificationConfigurationRepository, NotificationConfigurationRepository>();
        builder.Services.AddScoped<INotificationConfigurationReader, NotificationConfigurationReader>();

        builder.Services.AddScoped<IEmailService, SmtpEmailService>();
        builder.Services.AddScoped<INotificationQueueService, NotificationQueueService>();
        builder.Services.AddHostedService<NotificationProcessorService>();

        return builder;
    }
}
