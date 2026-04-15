using Mentoory.Access.Application.Services;
using Microsoft.Extensions.Logging;
using UAParser;

namespace Mentoory.Access.Infrastructure.Services;

public partial class UaParserUserAgentParser : IUserAgentParser
{
    private const string UnknownBrowser = "Navegador desconocido";
    private const string UnknownOs = "Sistema operativo desconocido";

    private readonly Parser _parser;
    private readonly ILogger<UaParserUserAgentParser> _logger;

    public UaParserUserAgentParser(ILogger<UaParserUserAgentParser> logger)
    {
        _parser = Parser.GetDefault();
        _logger = logger;
    }

    public (string BrowserName, string OperatingSystem) Parse(string? userAgentString)
    {
        if (string.IsNullOrWhiteSpace(userAgentString))
        {
            return (UnknownBrowser, UnknownOs);
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

            return (browser, os);
        }
        catch (Exception ex)
        {
            LogParsingFailed(ex, userAgentString);
            return (UnknownBrowser, UnknownOs);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse User-Agent string: {UserAgent}")]
    partial void LogParsingFailed(Exception ex, string userAgent);
}
