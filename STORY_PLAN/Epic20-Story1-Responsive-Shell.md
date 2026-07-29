# Epic 20 / Story 20.1 — Responsive shell

## Goal

Collapsible menu on small screens; **do not change desktop layout**.

Pattern: ERPCOM3 Epic 21 (drawer + hamburger).

## Approach

Reuse the ERPCOM3 mobile shell already present in EUROERP foundation:

| Piece | Behavior |
|-------|----------|
| Hamburger (`mobile-hamburger-btn`) | Visible only ≤768px; toggles drawer |
| Desktop sidebar toggle | Hidden on mobile |
| Top nav (`Principal`, `Vendas`, …) | Hidden in header on mobile; mirrored at top of drawer |
| Left sidebar | Fixed overlay drawer (`translateX`); backdrop closes it |
| Nav link click | Closes drawer |
| Desktop | Unchanged (media query only) |

## Implementation status

Already in tree from foundation port (`MainLayout`, `TopMenuInteractive`, `LeftMenuInteractive`, `LeftMenuItemNode`, `ILayoutStateService`, `app.css` mobile block, viewport meta).

**Polish in this story:**

1. Mobile CSS overrides desktop `main-layout-sidebar-collapsed` so drawer still opens at full width.
2. Hamburger `aria-expanded` / Abrir–Fechar labels; TopMenu re-renders on `LayoutState.OnChange`.
3. Slightly compact header user area on narrow screens (username can ellipsis).

## Out of scope

- Per-page form/table mobile redesign (rich screens stay scrollable).
- Changing desktop spacing, colors, or menu structure.

## Done when

- ≤768px: content uses full width; menu via hamburger drawer with top sections + left tree.
- >768px: identical to current desktop layout.
