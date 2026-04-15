using Mentoory.Notification.Contracts.Configuration;
using Mentoory.Notification.Application.Services;
using Mentoory.Notification.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mentoory.Notification.Infrastructure.Services;

public partial class NotificationProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationProcessorService> _logger;

    public NotificationProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int pollingIntervalSeconds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var configReader = scope.ServiceProvider.GetRequiredService<INotificationConfigurationReader>();
            pollingIntervalSeconds = await configReader.GetIntAsync(nameof(NotificationConfigurationKey.PollingIntervalSeconds), stoppingToken);
        }

        LogProcessorStarted(pollingIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(pollingIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<INotificationQueueService>();
                await queueService.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogProcessingError(ex);
            }
        }

        LogProcessorStopped();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification processor started with {IntervalSeconds}s polling interval")]
    partial void LogProcessorStarted(int intervalSeconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing pending notifications")]
    partial void LogProcessingError(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification processor stopped")]
    partial void LogProcessorStopped();
}
