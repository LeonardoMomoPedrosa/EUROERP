// Toggle sidebar collapsed state (called from Blazor TopMenuInteractive).
window.setSidebarCollapsed = function (collapsed) {
    var el = document.getElementById('main-layout-sidebar');
    if (!el) return;
    if (collapsed)
        el.classList.add('main-layout-sidebar-collapsed');
    else
        el.classList.remove('main-layout-sidebar-collapsed');
};
