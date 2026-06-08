namespace EUROERP.Web.Models.Menu;

public class MenuItemDto
{
    public string Label { get; set; } = string.Empty;
    public string? Route { get; set; }
    public List<MenuItemDto> Children { get; set; } = new();
    public string? RequiredRole { get; set; }
}
