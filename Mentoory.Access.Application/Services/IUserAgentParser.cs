namespace Mentoory.Access.Application.Services;

public interface IUserAgentParser
{
    (string BrowserName, string OperatingSystem) Parse(string? userAgentString);
}
