# Visual and behavioral UI acceptance protocol

## Snapshots

Capture each key screen in Windows/Linux at 560, 768, 1024, 1280, 1366, 1440 and 1920 widths; at 720 and 900 heights; light/dark and 150% scale. Snapshot fixtures for Chat empty/streaming/long history, Agent approval/active/failed, Models loading/list/details/download, Skills results/detail/install/update, Tasks empty/running/failed, Settings errors, Image workflow/queue, Git review with multiple files, command palette, sidebar drawer.

## Quality rubric (score each /5, no invented scores)

1. **Information architecture:** important action discoverable within two navigations; route title/breadcrumb consistent.
2. **Visual hierarchy:** clear heading, one primary CTA, legible metadata, intentional whitespace.
3. **Density:** useful information without miniature text or overwhelming panels.
4. **State clarity:** loading, ready, empty, offline, denied, failed, canceled and completed clearly different.
5. **Input/focus:** keyboard reachable, no surprise focus jumps, Esc topmost overlay, no click-through.
6. **Responsiveness:** no horizontal overflow at 560px, right inspector collapses, lists virtualized.
7. **Accessibility:** focus indicator, accessible name, tab order, contrast, reduced motion, screen reader announcement.
8. **Performance:** smooth streaming, bounded layout work, no repeated full rerenders, no phantom background callbacks.

## Automated tests

- Unit: VM state transitions, overlay stack, focus target resolver, popup placement, routing drafts, policy/approval, model library filters.
- Integration: 1000-item skill list, 10k models, duplicate model names, slow downloads, 429, network off, corrupt persistence, canceled file picker.
- UI automation: buttons are not occluded (bounds+hit test), no focus in hidden panels, backdrop hit-test toggles, route scroll and inspector width, child popover anchored/clamped.
- Browser-style visual regression concepts are transferable, but Uno testing must run actual WinUI/Skia Desktop app where applicable.
- Manual: two monitors, DPI change mid-window, RDP, narrow snapped mode, screen reader Narrator/Orca, Wayland/X11 variations, keyboard-only.

## Exit criteria

Do not approve UI release until: screenshot set captured and reviewed, no blocking overlay regressions, real Windows/Linux launch/keyboard tests, no ephemeral error loss, all keyboard- and pointer-visible controls perform their displayed action, and visual defects tracked and resolved rather than obscured with hardcoded sizes.
