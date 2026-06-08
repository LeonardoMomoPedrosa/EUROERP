namespace EUROERP.Web.Services;

public interface ILayoutStateService
{
    bool SidebarCollapsed { get; }
    bool MobileDrawerOpen { get; }
    void ToggleSidebar();
    void SetMobileDrawerOpen(bool open);
    event Action? OnChange;
}
