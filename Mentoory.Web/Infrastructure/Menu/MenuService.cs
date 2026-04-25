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
        var role = _httpContextAccessor.HttpContext?.User?.GetActiveRole();
        if (string.IsNullOrEmpty(role))
        {
            return Array.Empty<MenuItem>();
        }

        var visible = new List<MenuItem>();
        foreach (var item in MenuConfiguration.GetMenuItems())
        {
            if (!item.Roles.Contains(role))
            {
                continue;
            }

            if (item is not MenuGroup group)
            {
                visible.Add(item);
                continue;
            }

            // Children with an empty Roles array inherit the group's visibility; children
            // with an explicit role list render only when the active role matches. Groups
            // left with zero visible children are dropped — otherwise the view would render
            // an empty dropdown (spec 017 US6-5).
            var visibleChildren = group.Items
                .Where(c => c.Roles.Length == 0 || c.Roles.Contains(role))
                .ToArray();
            if (visibleChildren.Length == 0)
            {
                continue;
            }

            visible.Add(new MenuGroup(group.Title, group.Icon, group.Roles, visibleChildren));
        }

        return visible;
    }
}
