# Interaction invariants, failure states and surprising-overlay prevention

## Every interactive element
Explicit role, label, enabled/disabled reason, hover/focus/pressed, loading, success/failure feedback, keyboard invocation, Esc/back behavior, cancellation, idempotency, analytics/privacy decision, test ID and accessibility name. A click with no effect is a release blocker.

## Layer system
- Desktop windows and system prompts are tracked in a **single overlay coordinator** owned by application shell.
- Priority: nonmodal tooltip → popup/menu → anchored popover → drawer → blocking dialog → OS credential/file picker. Never open an untracked nested modal behind the current screen.
- Modal entry moves focus into its visible content; Tab is constrained to it. Closing via Cancel/Esc returns focus to opener, restores route and scroll, and leaves user data unchanged.
- Close only the topmost overlay. Outside-click closes only dismissible popovers, never approval/unsaved-edit dialogs.
- Dialog bounds must fit the current work area with scroll; no offscreen OK/Cancel at 760×560 or 150–200% text scaling. Handle Linux window manager differences and pop-up location.
- Pointer capture cannot leak through an overlay; underlying controls cannot be activated by the same click used to dismiss a popup.
- Nested permission/review flow queues requests in Approval Inbox instead of stacking prompts. Use non-blocking background task progress and stable notifications.
- Command palette / model picker / slash suggestions are mutually exclusive when they compete for keyboard focus; changing route dismisses anchored popovers.

## State truth table
| State | Visible behavior | Legal actions | Disallowed |
|---|---|---|---|
| Idle | No running indicator | Start/edit/options | Implied background work |
| Queued | Position, wait reason | Cancel/change priority if supported | Show 'running' |
| Running | Live step + elapsed observed | Stop/pause only if supported, navigate | Disable whole app |
| Approval required | Exact tool, args summary, scope & effects | Approve once/deny/inspect | Implicit approval, hidden modal |
| Cancel requested | Acknowledgement + pending cleanup | Inspect logs | Immediately claim cancelled |
| Cancelled | Final terminal state | Retry new run | Continue executing silently |
| Verification | Tests and source of evidence | Open outputs, inspect | Claim complete before tests |
| Failed recoverable | Last completed step, error, retry safety | Retry, edit, diagnostics | Generic 'oops' with no context |
| Failed terminal | Root reason + next action | Duplicate/start fresh | Auto-loop forever |
| Completed | Final artifact/diff/test status | Review, continue task | Claim changes applied if proposal only |
| Disconnected | Connection identity + reconnect | Retry/offline local | Pretend stream continues |

## Contextual UI details
- Status chip location stable; do not repeat 3 different statuses in header, inspector, composer with contradicting values.
- Model picker remains anchored above composer and searchable, supports arrows/Enter/Esc, caps scroll, preserves focus, shows provider+runtime+mode.
- Right-click chat opens native context menu with working rename/pin/archive/delete; delete confirmation names target, supports undo if retained.
- Discrete notices auto-dismiss only if nonactionable; errors requiring action persist in inbox/detail view.
- Skeleton/loading never shows stale content as success. Preserve prior user input until operation is acknowledged.
- Scroll follows tokens only while user is already at bottom; manual reading pauses autoscroll. Code blocks scroll horizontally without forcing chat width.
- Terminal and editor preserve scroll/cursor and selection while inspector changes.
- Windows/Linux file dialogs must not open behind main window; ensure proper window handles and focus restore.

## State tests
Use keyboard-only, mouse, high-DPI, rapid-click, OS switching, route changes during streaming, nested popovers, DPI changes, split resize, minimized/restored window, full screen, tool timeout, restart mid-operation, and stale workspace path. Every defect must produce a reproducible scenario and regression test before calling it fixed.
