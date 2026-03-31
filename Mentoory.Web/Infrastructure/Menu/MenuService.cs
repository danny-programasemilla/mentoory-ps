using Microsoft.AspNetCore.Http;

namespace Mentoory.Web.Infrastructure.Menu;

public class MenuService : IMenuService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MenuService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IReadOnlyList<MenuItem> GetVisibleMenuItems()
    {
        var role = _httpContextAccessor.HttpContext?.User?.FindFirst("ActiveRole")?.Value;
        if (string.IsNullOrEmpty(role))
        {
            return Array.Empty<MenuItem>();
        }

        return MenuConfiguration.GetMenuItems()
            .Where(item => item.Roles.Contains(role))
            .ToList();
    }
}
