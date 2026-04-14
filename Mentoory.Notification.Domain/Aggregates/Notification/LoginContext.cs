using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Notification.Domain.Aggregates.Notification;

public class LoginContext : ValueObject
{
    public LoginContext(string ipAddress, string browserName, string operatingSystem, bool isSuspicious)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("IP address cannot be empty.", nameof(ipAddress));
        }

        if (string.IsNullOrWhiteSpace(browserName))
        {
            throw new ArgumentException("Browser name cannot be empty.", nameof(browserName));
        }

        if (string.IsNullOrWhiteSpace(operatingSystem))
        {
            throw new ArgumentException("Operating system cannot be empty.", nameof(operatingSystem));
        }

        IpAddress = ipAddress;
        BrowserName = browserName;
        OperatingSystem = operatingSystem;
        IsSuspicious = isSuspicious;
    }

    private LoginContext()
    {
    }

    public string IpAddress { get; private set; } = null!;

    public string BrowserName { get; private set; } = null!;

    public string OperatingSystem { get; private set; } = null!;

    public bool IsSuspicious { get; private set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IpAddress;
        yield return BrowserName;
        yield return OperatingSystem;
        yield return IsSuspicious;
    }
}
