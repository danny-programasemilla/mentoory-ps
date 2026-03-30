using System.Reflection;

namespace Mentoory.Web.Services;

public class VersionProvider : IVersionProvider
{
    public VersionProvider()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        Version = assembly
                      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                  ?? assembly.GetName().Version?.ToString()
                  ?? "Unknown";
    }

    public string Version { get; }
}
