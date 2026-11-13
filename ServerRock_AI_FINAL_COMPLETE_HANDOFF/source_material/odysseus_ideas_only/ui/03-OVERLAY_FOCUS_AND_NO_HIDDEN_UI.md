# Fix “unexpected results behind pages”: overlay, hit-testing, dialog and focus invariants

## Why this specifically matters in current code

`MainPage.xaml` has a `SidebarBackdrop` (Grid.ColumnSpan=2) and a `CompanionOverlay` Canvas at page root. `MainPage.Layout.cs` toggles sidebar/scrim visibility; `MainPage.Companion.cs` repositions card manually; various feature files construct independent `ContentDialog`s. Settings and Model Library can have wide, scrollable nested content. There is no single declarative coordinator responsible for all overlays, animations, pointer capture and focus. This makes covering/overlapping/click-through cases a **risk to verify**, not a universally reproduced defect.

## Layer contract — explicit ordering

- Layer 0: App background, shell, persistent navigation.
- Layer 10: Routed page content, docked inspector, composer, console (all participate in layout and reserve space).
- Layer 20: Nonblocking toasts/status anchored to reserved safe area; pointer only on actual toast cards.
- Layer 30: Popovers, dropdown menus, command palette; anchored and clipped/flip to visible monitor bounds.
- Layer 40: Sidebar drawer **scrim** and the drawer above scrim. Scrim pointer capture active only while drawer is open. Scrim must intercept clicks to protected background and dismiss drawer, never allow a click to reach underlying action.
- Layer 50: Modal scrim and single active modal; modal higher than sidebar drawer and floating companion; keyboard focus trapped inside modal. Only a modal's explicitly supported secondary popup may exceed it.
- Layer 60: Native/system-owned file dialogs; ensure no app overlay steals focus or reopens while active.

**Companion**: place as docked/anchored adornment in non-modal workspace layer. In floating mode, only the sprite/card hit-test is enabled (Canvas does not intercept events across entire root). When a modal or drawer opens, hide, park or disable pointer handling on pet. Never allow pet on top of a confirmation, prompt box, popover or button.

## Recommended coordinator API (C# pseudocode, not compile-ready)

```csharp
public enum OverlayKind { Drawer, Popover, Modal, Toast }
public record OverlayToken(Guid Id, OverlayKind Kind, string OwnerRoute);
public interface IOverlayCoordinator {
  Task<OverlayToken> OpenAsync(OverlayRequest request, CancellationToken token);
  Task CloseAsync(OverlayToken token, OverlayCloseReason reason);
  bool IsBlockingOverlayOpen { get; }
  void RestoreFocus(OverlayToken token);
  void CloseOwnedBy(string routeId);
}
```

The coordinator owns one blocking overlay at a time; requests to open another queue, replace or fail with a visible reason. Never call `ContentDialog.ShowAsync` while another is active on same XamlRoot. `CloseAsync` can finish exactly once and cancels pending animation callbacks. Ignore late results if owner route/run is disposed or the token changed. Restore focus to trigger if still visible/enabled; otherwise to page heading or composer. Esc closes topmost dismissible overlay, not arbitrary underlying page. An irreversible approval dialog does NOT dismiss on scrim click.

## Hit testing and safety

- No transparent full-screen overlay with `IsHitTestVisible=true` unless it is an *active blocking scrim*. On dismiss, set visibility and hit testing off atomically before returning focus.
- Do not set only opacity to 0 for dismissed overlays; transparent visual can still intercept pointer events. Remove from root or collapse, clear pointer capture, close popover, stop animation.
- Browser-inspired z-index patterns do not translate directly to Uno; use explicit `Canvas.ZIndex` / root child order and test on actual target platforms. Make root overlay host separate from route content.
- Popovers must not render behind full-screen page, under the footer, or outside window bounds: compute anchor transform, clamp to effective viewport, flip orientation if insufficient space, reflow on resize and display scale changes.
- Resize should be transactional: recompute layout before overlay placement; dispose stale anchors; account for top title bar and safe area and bottom composer.
- Keep only **one vertical scroll owner per functional region** (e.g., Settings content scroll, Skills result list). Avoid nested non-scrolling child inside fixed-height modal. Use min size and available width binding, not blind `MinWidth=360` inside narrow dialogs.
- For drags: capture only pointer on drag handle, preserve content hit-testing, cancel on route close, Esc and lost capture; clamp visible title/grab strip to window.

## Dialog lifecycle table

| Action | Expected overlay behavior | Focus after |
|---|---|---|
| Open settings | Navigate to settings route, no scrim | Settings heading / active category |
| Open model selector from chat | Compact anchored popup | Return to model selector |
| Open full Models | Navigate to routed Models page | Models search |
| Open skill details | Inspector/drawer reserves width | Skill row on close |
| Request destructive command approval | One modal with explicit confirmation and exact diff | Approved step / original trigger |
| Open file picker | Suspend app popovers; native dialog above all | Upload control |
| Show error | Nonblocking toast plus persistent error event | Preserve current input/focus |
| Open compact sidebar | One drawer+scrim, body noninteractive | Original menu toggle on close |
| Show pet | Noninteractive decorations except sprite action | Doesn't steal focus |

## Test matrix: screenshots AND interaction

Sizes: 560x700, 768x768, 1024x768, 1280x720, 1366x768, 1440x900, 1920x1080; 100%, 125%, 150%, 200% scale; light/dark/high contrast; Windows and Linux. Test keyboard-only Tab/Shift+Tab/Esc/Enter/Space, screen reader, IME, long translated labels, 200% text scaling, OS screen edge. Use automation element bounds and hit-test target: clicking a modal action must never invoke a page action behind it. Dismissed backdrop must no longer receive input; reopened modal must not be canceled by delayed animation from previous close. Test window resize mid-dialog, navigating while an async search resolves, simultaneous run notifications and pet, narrow settings, and full model list at 10,000 results.

## Acceptance checklist

- [ ] No modal has hidden buttons or content beyond visible/scrollable bounds.
- [ ] No overlay blocks or accidentally activates underlying controls.
- [ ] Every blocking overlay has a single owner, close method and focus-return target.
- [ ] Pointer capture/animation/cancellation are released when overlay closes.
- [ ] Companion never obscures composer, code diff, permission dialog or content being interacted with.
- [ ] Escape closes only the topmost dismissible layer.
- [ ] Footer, header, scrolling body, nav and inspector remain reachable at 560px width.
- [ ] UI and source architecture tests cover behavior, not just matching strings.

Odysseus reference: `static/js/modalManager.js`, `static/js/modalVisibility.js`, `static/js/modalSnap.js`, `static/js/sidebar-layout.js`, `tests/test_modal_reopen_lifecycle.py`, `tests/test_modal_dock_composer_clearance.py`. Study contracts; don't port its JS into Uno.
