using Mentoory.Shared.Application.Notifications;
using RazorLight;

namespace Mentoory.Tenant.Infrastructure.Services;

public class TenantTemplateRenderer : ITemplateRenderer
{
    private readonly RazorLightEngine _engine;

    public TenantTemplateRenderer()
    {
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(
                typeof(TenantTemplateRenderer).Assembly,
                "Mentoory.Tenant.Infrastructure.Templates")
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync(string templateName, object model)
    {
        return await _engine.CompileRenderAsync(templateName, model);
    }
}
