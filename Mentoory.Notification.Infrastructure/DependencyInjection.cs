using Mentoory.Notification.Application.Services;
using Mentoory.Notification.Domain.Repositories;
using Mentoory.Notification.Application.Configuration;
using Mentoory.Notification.Infrastructure.Configuration;
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

        builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
        builder.Services.Configure<NotificationSettings>(builder.Configuration.GetSection("Notification"));

        builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
        builder.Services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();

        builder.Services.AddScoped<IEmailService, SmtpEmailService>();

        builder.Services.AddSingleton<ITemplateRenderer, RazorLightTemplateRenderer>();
        builder.Services.AddSingleton<ILoginContextParser, UaParserLoginContextParser>();

        builder.Services.AddScoped<INotificationQueueService, NotificationQueueService>();
        builder.Services.AddHostedService<NotificationProcessorService>();

        return builder;
    }
}
