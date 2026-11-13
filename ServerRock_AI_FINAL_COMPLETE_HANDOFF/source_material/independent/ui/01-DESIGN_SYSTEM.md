# Server Rock desktop design system — visual direction and implementation

## Product principle

The app should feel like a focused, modern AI desktop workbench: clear navigation, generous space, consistent components, precise current state. Use an understated dark-neutral or light-neutral palette with ONE restrained accent; avoid rainbow buttons, cramped toolbars, and gimmicky overlays. A decorative pet is optional and must never block tools. Give the UI professional utility comparable to modern code editors and AI workspaces, while preserving Server Rock identity.

## Proposed page shell at 1440x900

- **Left rail 64px**: navigation icons: Home/Chat, Agent, Projects, Models, Skills, Images, Tasks, Settings; active route visibly highlighted. Tooltip + full accessible name; pin/unpin expanded sidebar.
- **Optional expandable navigation 220-264px**: conversation history, project tree, favorites; can collapse. No automatically opening drawers on every route change.
- **Main panel** flexible ≥ 420px: persistent header with breadcrumb, title, project selector, privacy/model status, primary action; page content owns scroll.
- **Right inspector 300-360px**, optional/resizable: context, artifacts, model controls, agent activity, file diffs; hidden when content width below threshold. Inspector must **reserve layout width**, never float over chat.
- **Bottom composer** anchored within Chat/Agent content only, not on top of Settings/Marketplace pages. Multiline text grows only to 30% of window height; chat remains scrollable.
- **Activity tray** collapsible at bottom of Agent/Projects: logs/tests/terminal with drag resize, not always-visible modal.

## Tokens (starting design choices, adjust after contrast check)

Colors (light): window #F5F7FA; surface #FFFFFF; elevated #FFFFFF; sidebar #F0F3F7; border #DDE3EA; primary text #16202A; secondary #596776; accent #267BD8; accent hover #1769C3; success #16734C; warning #A16414; error #B72F3C.

Colors (dark): window #10151B; surface #171E26; elevated #212B36; sidebar #111923; border #32404E; primary text #ECF2F8; secondary #B2BEC9; accent #6BADF3; success #63CC9A; warning #F3C06D; error #F68D9B.

Typography: system UI font (Segoe UI Variable Windows; appropriate UI sans Linux), 13-14px body at 100%, 12px metadata, 16px group title, 20-24px page title, 32px landing heading, 13px monospace code. Line-height ~1.4-1.55 for body. No letter-spaced all-caps headings everywhere. Avoid replacing actual icon assets with ambiguous Unicode characters.

Spacing scale: 4, 8, 12, 16, 24, 32px. Outer page gutters 24px desktop and 12-16px compact. Control min height 34-40px pointer; accessibility touch targets ≥44px where appropriate. Border radius 8px controls, 12px cards, 14px panels. Use subtle 1px borders, not heavy shadows. High-contrast theme overrides tokens, never rely on color alone for state.

## Reusable component vocabulary

`AppShell`, `NavigationRail`, `SectionSidebar`, `Breadcrumbs`, `PageHeader`, `PageBody`, `InspectorPanel`, `StatusChip`, `ModelSelector`, `SearchField`, `FilterBar`, `VirtualizedResults`, `DetailsPane`, `ArtifactCard`, `TaskProgress`, `PermissionSummary`, `DiffViewer`, `TerminalLog`, `EmptyState`, `ErrorState`, `LoadingSkeleton`, `InlineWarning`, `Toast`, `Modal`, `Confirmation`, `CommandPalette`, `ToolTip`, `ResizableSplitPane`, `VirtualizedChatTimeline`.

For each component define **min width/height, pointer behavior, keyboard behavior, focus, semantically named accessibility role, loading/empty/error states, cancellation, and disposal**. Shared tokens live in ResourceDictionary theme files, not repeated hex values in dynamic C# view constructors.

## Usability heuristics

- The current working model, connected endpoint, privacy state and executing task are always visible without opening Settings.
- Destructive or external operations show what will change, destination, impacts and undo availability. Dry-run preview should be the default.
- No primary navigation screen is just a large blocking dialog. Use pages for work, dialogs for decisions.
- No button that says Install, Run, Sync or Update may simply update a decorative label; it must perform the action and reflect verified result.
- Async status is a persistent task row with a clear outcome, not a transient generic banner that can disappear.
- Empty state always describes prerequisite and provides ONE clear CTA. Disabled actions explain why.

## File migration

1. `App.xaml`: extract color/spacing/type styles to `Themes/Tokens.Light.xaml`, `Tokens.Dark.xaml`, `Components.xaml` with Uno-supported ThemeResource/StaticResource conventions.
2. `MainPage.xaml`: replace monolithic page body with shared `AppShell` and route host; maintain shell while navigating.
3. `SidebarView.xaml`: split primary nav rail from history sidebar; add true route selection semantics.
4. `UiTheme.cs`: keep resource lookup helpers only and eliminate ad-hoc `new SolidColorBrush(ColorHelper...)` where possible.
5. `MainPage.Settings.cs`, `MainPage.ModelLibrary.cs`, `MainPage.PluginsAndSkills.cs`: move full-screen experiences to `Pages/` with view models.
6. `MainPage.Chat.cs`: virtualized timeline + composer view model, with throttled updates.
