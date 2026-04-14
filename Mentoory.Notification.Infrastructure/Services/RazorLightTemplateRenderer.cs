using RazorLight;

namespace Mentoory.Notification.Infrastructure.Services;

public class RazorLightTemplateRenderer : ITemplateRenderer
{
    private readonly RazorLightEngine _engine;

    public RazorLightTemplateRenderer()
    {
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(
                typeof(RazorLightTemplateRenderer).Assembly,
                "Mentoory.Notification.Infrastructure.Templates")
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync(string templateName, object model)
    {
        return await _engine.CompileRenderAsync(templateName, model);
    }
}
