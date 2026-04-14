namespace Mentoory.Web.Infrastructure.Menu;

public class MenuItem
{
    public MenuItem(string title, string url, string icon, string[]? roles = null)
    {
        Title = title;
        Url = url;
        Icon = icon;
        Roles = roles ?? Array.Empty<string>();
    }

    public string Title { get; }

    public string Url { get; }

    public string Icon { get; }

    public string[] Roles { get; }

    public int? BadgeCount { get; set; }
}
