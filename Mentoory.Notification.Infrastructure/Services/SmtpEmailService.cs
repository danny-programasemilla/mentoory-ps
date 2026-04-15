using MailKit.Net.Smtp;
using MailKit.Security;
using Mentoory.Notification.Contracts.Configuration;
using Mentoory.Notification.Domain.Enums;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Mentoory.Notification.Infrastructure.Services;

public partial class SmtpEmailService : IEmailService
{
    private readonly INotificationConfigurationReader _configReader;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(INotificationConfigurationReader configReader, ILogger<SmtpEmailService> logger)
    {
        _configReader = configReader;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var host = await _configReader.GetStringAsync(nameof(NotificationConfigurationKey.SmtpHost), cancellationToken);
        var port = await _configReader.GetIntAsync(nameof(NotificationConfigurationKey.SmtpPort), cancellationToken);
        var username = await _configReader.GetStringAsync(nameof(NotificationConfigurationKey.SmtpUsername), cancellationToken);
        var password = await _configReader.GetStringAsync(nameof(NotificationConfigurationKey.SmtpPassword), cancellationToken);
        var fromAddress = await _configReader.GetStringAsync(nameof(NotificationConfigurationKey.SmtpFromAddress), cancellationToken);
        var fromName = await _configReader.GetStringAsync(nameof(NotificationConfigurationKey.SmtpFromName), cancellationToken);
        var useSsl = await _configReader.GetBoolAsync(nameof(NotificationConfigurationKey.SmtpUseSsl), cancellationToken);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        var secureSocketOptions = useSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, secureSocketOptions, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        LogEmailSent(to, subject);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent to {To} with subject '{Subject}'")]
    partial void LogEmailSent(string to, string subject);
}
