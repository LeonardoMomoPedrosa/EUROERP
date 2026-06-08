namespace EUROERP.Web.Services;

public class LayoutStateService : ILayoutStateService
{
    public bool SidebarCollapsed { get; private set; }
    public bool MobileDrawerOpen { get; private set; }

    public void ToggleSidebar()
    {
        SidebarCollapsed = !SidebarCollapsed;
        OnChange?.Invoke();
    }

    public void SetMobileDrawerOpen(bool open)
    {
        if (MobileDrawerOpen == open) return;
        MobileDrawerOpen = open;
        OnChange?.Invoke();
    }

    public event Action? OnChange;
}
