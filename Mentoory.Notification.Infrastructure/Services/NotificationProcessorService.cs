using Mentoory.Notification.Application.Services;
using Mentoory.Notification.Application.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mentoory.Notification.Infrastructure.Services;

public partial class NotificationProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationProcessorService> _logger;

    public NotificationProcessorService(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogProcessorStarted(_settings.PollingIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds));

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
