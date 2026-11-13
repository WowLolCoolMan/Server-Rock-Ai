# Keyboard, command palette, focus and accessibility

These shortcut defaults are proposed; check collisions with Windows/Linux/Uno and make configurable. Announce key conflicts in Settings. Cmd-specific/macOS is outside committed targets.

| Action | Default | Context |
|---|---|---|
| Command palette | Ctrl+K | Anywhere except OS modal |
| Quick open file | Ctrl+P | Active project |
| Advanced command palette | Ctrl+Shift+P | Anywhere |
| New chat | Ctrl+N | App |
| New agent task | Ctrl+Shift+N | Project/app |
| Settings | Ctrl+, | App |
| Search in current thread | Ctrl+F | Chat |
| Toggle sidebar | Ctrl+B | Shell |
| Toggle inspector | Ctrl+Alt+I | Shell |
| Toggle terminal dock | Ctrl+` | Coding workspace |
| Switch open tab | Ctrl+Tab / Ctrl+Shift+Tab | Workspace |
| Model picker | Ctrl+Shift+M | Composer |
| Send | Enter | Composer unless multiline preference |
| Newline | Shift+Enter | Composer |
| Close topmost overlay | Esc | App |
| Focus command output | F6 cycling | Workspace regions |

## Tests
- Navigate all major paths using keyboard only; focus is visible, logical and never trapped behind a dismissed dialog.
- Every icon button has accessible name + optional tooltip + meaningful disabled reason.
- List rows support arrow navigation, Enter/Space activation, virtualized list semantics and announcements when results load.
- Approval text is understandable by screen reader, including command, file count, risk level and allow/deny scope.
- When streaming, announce sensible summary/status updates without speaking every token or stealing focus.
- Status colors always accompanied by text/shape. Validate WCAG AA contrast on actual platform renders.
- User-adjustable density/text size and high-contrast/OS screen scaling do not hide controls.
- Drag/drop-only workflows must have keyboard equivalents. Workflow canvas requires accessible nonvisual ordered node/edge editor.
- Cross-platform accessibility: Windows Narrator/Automation peers and Linux accessibility tree/AT-SPI with a live desktop environment where possible.
