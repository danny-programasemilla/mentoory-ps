using Mentoory.Notification.Domain.Aggregates.Notification;

namespace Mentoory.Notification.Application.Services;

public interface ILoginContextParser
{
    LoginContext Parse(string? userAgentString, string ipAddress, bool isSuspicious);
}
