namespace Mentoory.Shared.Application.Notifications;

public interface IEmailLayoutWrapper
{
    string WrapInBrandLayout(string innerHtml);
}
