// Toggle sidebar collapsed state (called from Blazor TopMenuInteractive).
window.setSidebarCollapsed = function (collapsed) {
    var el = document.getElementById('main-layout-sidebar');
    if (!el) return;
    if (collapsed)
        el.classList.add('main-layout-sidebar-collapsed');
    else
        el.classList.remove('main-layout-sidebar-collapsed');
};

/** Scroll the main content pane to top (layout scrolls .main-layout-content, not window). */
window.scrollMainContentToTop = function () {
    var el = document.querySelector('.main-layout-content');
    if (el)
        el.scrollTo({ top: 0, behavior: 'smooth' });
    else
        window.scrollTo({ top: 0, behavior: 'smooth' });
};
