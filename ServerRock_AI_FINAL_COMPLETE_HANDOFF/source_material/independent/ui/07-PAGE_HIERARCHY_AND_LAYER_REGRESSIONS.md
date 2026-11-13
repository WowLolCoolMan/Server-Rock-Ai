# Original Server Rock UI: every page is usable, no hidden-click surprises

## Guiding interaction model

A native desktop **workspace** uses routed pages, not a stack of oversized `ContentDialog` windows. An active page owns its state; a system dialog belongs to the foreground route and cannot trigger a stale handler behind a different route. Preserve the brand's distinctive rock/crystal mark and controlled, clear design language without duplicating another app's interface.

### Route destinations

| Route | Always-visible main action | Main content | Secondary inspector | State to persist |
|---|---|---|---|---|
| Home | New Chat / Continue task | Current providers, recent projects, running tasks | Optional quick diagnostics | Recent destination |
| Chat | Send / Stop | Virtualized transcript, source refs, tool cards | Context/docs (opt-in) | Draft, model, scroll, attachments |
| Agent | Start / Pause / Resume / Cancel | Goal, phase timeline, observed results, approvals | Files, diff, tests, permissions | Run ID, pending approvals, status |
| Projects | Open / Create project | Files, repository health, recent runs | Git changes, project settings | Selected project and directory |
| Models | Select / Download / Remove | Installed, Discover, Compare, Activity | Details, compatibility, disk estimates | Source, filters, selected model |
| Skills | Inspect / Test / Enable | Installed, Discover, Drafts, Updates | License, permissions, version diff | Filters and selection |
| Images | Generate / Cancel job | Workflows, queue, outputs | Workflow parameters and model info | Queue and prompt |
| Tasks | Inspect / Pause / Cancel | Running, scheduled, completed, failed | Activity timeline and artifact links | Filters and selected task |
| Settings | Save / Revert | Categories within persistent page | Inline help only | Uncommitted form draft |
| Diagnostics | Recheck / Export | Backend connectivity, troubleshooting, logs | Error details | Filtered event IDs |

### Required behavior for every route

- All primary page content exists within the route's own bounds; no full-screen invisible canvas can block it.
- Switching routes never closes or restarts an agent or image job unless it is explicitly bound to that route with documented cancellation rules. Page component disposal may stop UI subscriptions; it must not kill the run.
- Settings drafts use `Original` and `WorkingCopy`; Cancel discards, Save validates all fields **before** mutation. On an invalid input, keep form and focus the field rather than closing the dialog and announcing failure elsewhere. Persist only after successful validation.
- If a new route opens while a modal decision is pending, either keep the modal active and block route changes or close it with an explicit reject/cancel outcome. Never leave half-hidden modal controls.
- Modal host owns a single blocking scrim; drawer host owns one drawer scrim. Opening a modal closes or suspends popovers and drawer first, then guarantees topmost focus. A dialog never opens under a drawer or under another dialog.
- Escape closes the highest dismissible layer only. Closing a layer restores focus to its source if still visible; otherwise use route heading or last focusable location.
- Popup menus clamp to actual viewport and flip above/left when insufficient room. Reposition on resize, DPI change, zoom and navigation; close on anchor disposal.
- Floating pet is optional and cannot accept hit tests over its transparent Canvas. It parks or becomes noninteractive whenever scrims/menus/important controls are present.
- Scrolling ownership is explicit: shell navigation has one scroll area, route content has one, inspectors have one; do not nest fixed-height unscrollable content within another modal scroll.
- Running notifications use an activity center and durable job records; removing toast never removes error or task state.

## 30 deterministic native UI regression scenarios

1. On Home at 560x700, open sidebar, tap scrim. Sidebar closes and original page action has **not** fired.
2. Open sidebar and Settings; no leftover sidebar scrim intercepts Settings clicks.
3. From Chat open model selector; press Escape. Composer stays focused and draft remains.
4. Open a dropdown and resize the window shorter; menu repositions within viewport.
5. Open Settings, edit endpoint then Cancel. Old endpoint remains unchanged.
6. Open Settings, enter invalid cloud endpoint with local-only enabled, click Save. Settings stays open, nothing saved, invalid field is focused.
7. Open Settings at 560px; every field and bottom action is scrollably reachable.
8. Open and close Settings 25 times. No leaked event handlers, phantom dialogs or duplicated callbacks.
9. While closing one modal, immediately reopen it. A prior close-animation callback cannot close the new modal.
10. Attempt to open two approval dialogs simultaneously; second is queued/rejected visibly without losing its request.
11. Click behind an approval modal; no underlying file write, send, delete, or Git action fires.
12. Press Escape on a non-dismissible destructive approval; no inadvertent approval or route transition occurs.
13. Open file picker while popup active; popup suspends and cannot reclaim focus above picker.
14. Open Skills, inspect a skill; inspector reserves width rather than covering the Install control.
15. From a 1024px window open the right inspector; main panel retains minimum readable width or inspector becomes tabbed.
16. Open agent run, navigate to Images, return. Same run still exists with accurate phase and permissions.
17. Generate image then navigate to Tasks; queue status persists and remains cancellable by exact job ID.
18. Click Stop on an agent run while two jobs are active; only selected run receives cancellation.
19. Open Companion and move it over a control; transparent canvas must not absorb clicks away from visible sprite.
20. With Companion visible, open permission modal; Companion cannot cover modal or receive input.
21. Resize window during pet dragging; pointer capture releases safely on loss, pet stays in bounds.
22. Toggle sidebar at 1280/1320 breakpoint repeatedly; no invisible scrim or lost navigation.
23. Scroll up long Chat then receive 5000 streamed tokens; user scroll remains pinned where they are reading.
24. At 200% scaling, composer does not obscure newest message or essential buttons.
25. At 200% text scale, Settings section nav and form stay independently reachable.
26. From Search switch rapidly between results while an async fetch completes; stale result cannot mutate the new route.
27. Start with no provider; show clear Connect Model CTA, not a blank chat panel or fake connected badge.
28. Simulate provider disconnection midstream; show failed/degraded state and preserve partial response + retry option.
29. Run high-contrast, keyboard-only navigation through every main action, then use screen-reader labels to verify state.
30. Navigate between every route 100 times, leaving drawers, popovers, timers and async operations active; report exception count, leaked handlers, duplicate actions, focus failures and hit-test results.

### Evidence standard

Use automated native UI interaction where supported, record before/after screenshots at 560x700, 768x768, 1024x768, 1366x768, 1920x1080 on Windows and Linux, collect UI tree bounds and actual hit-test target for actionable elements. A screenshot of an attractive page is **not** proof that its hidden background layers aren't stealing clicks. A successful build is **not** proof that focus, layering or cancellation work. On missing platform availability mark it **unverified** rather than pass.
