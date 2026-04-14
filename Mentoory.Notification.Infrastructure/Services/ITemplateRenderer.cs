namespace Mentoory.Notification.Infrastructure.Services;

public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templateName, object model);
}
