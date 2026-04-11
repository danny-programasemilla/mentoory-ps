namespace Mentoory.Web.Infrastructure.Menu;

public interface IMenuService
{
    IReadOnlyList<MenuItem> GetVisibleMenuItems();
}
