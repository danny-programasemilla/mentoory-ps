using MailKit.Net.Smtp;
using MailKit.Security;
using Mentoory.Notification.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Mentoory.Notification.Infrastructure.Services;

public partial class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        var secureSocketOptions = _settings.UseSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, secureSocketOptions, cancellationToken);
        await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        LogEmailSent(to, subject);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent to {To} with subject '{Subject}'")]
    partial void LogEmailSent(string to, string subject);
}
