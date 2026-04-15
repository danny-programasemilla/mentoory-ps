namespace Mentoory.Shared.Application.Notifications;

public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templateName, object model);
}
