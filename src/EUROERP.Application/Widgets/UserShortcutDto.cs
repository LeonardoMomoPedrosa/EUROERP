namespace EUROERP.Application.Widgets;

/// <summary>User-selected shortcut (route + display order). Label is resolved from menu when rendering.</summary>
public class UserShortcutDto
{
    public string Route { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
