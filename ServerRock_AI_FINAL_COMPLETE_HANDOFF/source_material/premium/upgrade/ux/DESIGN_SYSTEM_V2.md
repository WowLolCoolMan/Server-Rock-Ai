# Premium native Uno design system — implement without a second UI stack

## Intent
A calm, high-density, precision-oriented desktop application with the polish and discoverability associated with premium developer tools. Support user-selectable **light/dark** and a high-contrast mode once implemented; the current repo documents **light only**, so dark is a planned feature. Follow existing `DESIGN.md` and reuse `App.xaml` + `UiTheme.cs` tokens; avoid inline hex/C# style duplication.

## Example token specification (proposed values, not current implementation)
| Role | Dark candidate | Light candidate | Notes |
|---|---|---|---|
| app/canvas | `#0D1015` | `#F6F8FB` | Main workspace backdrop |
| surface/base | `#151922` | `#FFFFFF` | Cards/dialogs/panes |
| surface/raised | `#1E2430` | `#EFF2F7` | Hover, secondary surfaces |
| sidebar | `#11151C` | `#ECF2F9` | Persistent navigation |
| border | `#303744` | `#D4DBE5` | Low-chrome separators |
| text/primary | `#EDF2FA` | `#19202B` | High-contrast readable text |
| text/secondary | `#A8B3C3` | `#526173` | Verify WCAG contrast per role |
| accent | `#949FFF` | `#494AC0` | User-selectable within contrast constraints |
| success | `#65CAA8` | `#227657` | Status plus text, not color alone |
| warning | `#E5B76E` | `#92560F` | Caution, needs meaningful icon |
| danger | `#E68B96` | `#B23D49` | Destructive action |

These are design candidates; test actual color pairs for WCAG AA and adapt to Uno resource dictionaries. Do not rely on token tables without validating rendered contrast.

## Typography and sizing
- UI font: native Segoe UI on Windows, suitable Linux UI fallback; code mono with reliable fallback. Provide user text scaling.
- Core sizes: 11 metadata, 12 supporting, 13 compact controls, 14 body, 15–16 reading, 18 section, 24 page, 30 empty-state. No gratuitous huge headings inside dense panels.
- Spacing rhythm: 4, 8, 12, 16, 24, 32; component height 30–36 compact, 36–40 comfortable; 44+ for touch accessibility where needed.
- Corners: 6 small, 8 medium, 12 large; restrained shadows on popovers; hairline separators over full card borders wherever appropriate.
- Left rail 52–60; tree 232–292; inspector 280–390; activity/terminal dock 180–360. These are targets not hard constraints; use adaptive layout.
- Reading/chat column ~720–860 maximum; code/terminal fills remaining width. Composer aligns with reading column, auto-grows with capped height.
- Use actual icons from the existing Fluent/Uno pipeline, consistent stroke/size. No emoji/text glyphs as control icons.

## Rich native component library to implement/reuse
ApplicationShell, CommandBar, NavigationRail, TreeSidebar, ContextInspector, SplitPane, DockTabBar, TaskStatusBadge, ModelIdentityChip, ModelPicker, CommandPalette, BreadcrumbBar, InlineActionRow, MessageBubble, StreamingCaret, ToolTraceFoldout, CodeBlock, FileTree, EditorTab, DiffViewer, ApprovalCard, TerminalView, WorkflowCanvas, SkillCard, SkillDetail, ConnectionCard, EmptyState, Toast, NotificationInbox, FilePickerBridge, ConfirmDialog, ProgressRow, RecoveryPanel, VirtualList, DataTable, SettingsSection, KeybindingRecorder.

Each component needs variants: default, hovered, focus-visible, selected, disabled, busy, error, loading, empty. Input components must have ARIA-equivalent native automation peers/accessible names. Reuse tokens and state machines rather than duplicating custom controls in individual dialogs.

## Motion
- Subtle 120–200ms opacity/position for transient surfaces; no distracting spring animations on code. Disable/shorten motion under OS reduced-motion setting.
- No layout shifts caused by streaming tokens or dismissing an unrelated toast. Preserve cursor focus during live rerender.
- Skeletons match content footprint; large downloads show true byte/progress if known, otherwise labeled indeterminate state.

## Layout presets
- **Focus:** chat or editor central, inspector closed, side tree collapsible.
- **Balanced:** navigation + center + inspector.
- **Engineering:** explorer + editor/diff + terminal + agent/approvals.
- **Multi-agent:** task lanes and queue with per-agent status; open relevant inspector on selection.
Persist active preset, sizes, split ratios and open tabs by workspace, with Restore Defaults.

## Visual QA
Compare actual native screenshots at 1440×900, 1024×768, 760×560, Linux 125/150% scale, Windows 150/200% scale, dark/light/high contrast and reduced-motion. Verify truncation, popover z-order, scroll, focus, model picker, terminal selection, no clipped buttons. Do not judge polish from static mockups alone.
