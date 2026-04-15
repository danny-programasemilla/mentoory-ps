using Mentoory.Shared.Application.Notifications;
using RazorLight;

namespace Mentoory.Access.Infrastructure.Services;

public class AccessTemplateRenderer : ITemplateRenderer
{
    private readonly RazorLightEngine _engine;

    public AccessTemplateRenderer()
    {
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(
                typeof(AccessTemplateRenderer).Assembly,
                "Mentoory.Access.Infrastructure.Templates")
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync(string templateName, object model)
    {
        return await _engine.CompileRenderAsync(templateName, model);
    }
}
