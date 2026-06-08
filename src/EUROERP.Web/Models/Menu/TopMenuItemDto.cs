namespace EUROERP.Web.Models.Menu;

public class TopMenuItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<MenuItemDto> Children { get; set; } = new();
    public string? RequiredRole { get; set; }
}
