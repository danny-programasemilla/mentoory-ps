using Mentoory.Notification.Application.Services;
using Mentoory.Notification.Domain.Aggregates.Notification;
using Microsoft.Extensions.Logging;
using UAParser;

namespace Mentoory.Notification.Infrastructure.Services;

public partial class UaParserLoginContextParser : ILoginContextParser
{
    private const string UnknownBrowser = "Navegador desconocido";
    private const string UnknownOs = "Sistema operativo desconocido";

    private readonly Parser _parser;
    private readonly ILogger<UaParserLoginContextParser> _logger;

    public UaParserLoginContextParser(ILogger<UaParserLoginContextParser> logger)
    {
        _parser = Parser.GetDefault();
        _logger = logger;
    }

    public LoginContext Parse(string? userAgentString, string ipAddress, bool isSuspicious)
    {
        if (string.IsNullOrWhiteSpace(userAgentString))
        {
            return new LoginContext(ipAddress, UnknownBrowser, UnknownOs, isSuspicious);
        }

        try
        {
            var clientInfo = _parser.Parse(userAgentString);

            var browser = string.IsNullOrWhiteSpace(clientInfo.UA.Family)
                ? UnknownBrowser
                : clientInfo.UA.Family;

            var os = string.IsNullOrWhiteSpace(clientInfo.OS.Family)
                ? UnknownOs
                : clientInfo.OS.Family;

            return new LoginContext(ipAddress, browser, os, isSuspicious);
        }
        catch (Exception ex)
        {
            LogParsingFailed(ex, userAgentString);
            return new LoginContext(ipAddress, UnknownBrowser, UnknownOs, isSuspicious);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse User-Agent string: {UserAgent}")]
    partial void LogParsingFailed(Exception ex, string userAgent);
}
