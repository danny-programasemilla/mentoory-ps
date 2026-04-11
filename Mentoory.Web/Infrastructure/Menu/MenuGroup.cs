namespace Mentoory.Web.Infrastructure.Menu;

public class MenuGroup : MenuItem
{
    public MenuGroup(string title, string icon, string[] roles, MenuItem[] items)
        : base(title, "#", icon, roles)
    {
        Items = items;
    }

    public IReadOnlyList<MenuItem> Items { get; }
}
