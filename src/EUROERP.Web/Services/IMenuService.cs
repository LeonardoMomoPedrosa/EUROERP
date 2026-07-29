using EUROERP.Web.Models.Menu;

namespace EUROERP.Web.Services;

public interface IMenuService
{
    MenuConfigDto GetMenuConfig();

    /// <summary>
    /// Returns the top-menu id whose tree best matches the current path (longest route prefix), or null.
    /// </summary>
    string? ResolveTopItemIdForPath(string path);

    /// <summary>
    /// Flat list of every menu entry that has a route, labelled with its breadcrumb (e.g. "Principal » Produtos » Cadastro").
    /// </summary>
    IReadOnlyList<(string Label, string Route)> GetLinkableMenuItems();
}
