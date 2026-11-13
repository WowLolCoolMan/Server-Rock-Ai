# Premium desktop UI — routes, composition, navigation and behavioral contract

## Visual direction

A refined professional desktop app, not a generic web dashboard or overloaded IDE clone. Restrained tonal surfaces, distinctive first-party Server Rock identity, high-quality iconography and typography, delightful but short motion. Support light and dark themes with semantic design tokens in Uno `App.xaml`/`UiTheme.cs`; theme choices must not change interaction semantics. Prefer stable component metrics and grid layout to hand-positioned overlays. No stolen Codex/Steam/Discord/Claude assets or pixel-for-pixel imitation.

## Window anatomy

1. **Title/command strip:** logo, project breadcrumb, back/forward, global command palette, notifications/activity, profile and window controls (use platform conventions).
2. **Persistent left rail:** Home, Chat, Projects/Agent, Workflows/Tasks, Models, Skill Marketplace, Connections, Activity. Pin/reorder favorites, expose compact labels/tooltips, collapse without losing route state.
3. **Secondary contextual sidebar:** project folders, chats, branches or skill categories. Persist widths per workspace; allow keyboard resizing and clear hide/show.
4. **Central routed work surface:** rich conversations, workbench, editor, marketplace, diagnostics or settings. All pages are real routes, not stacks of unrelated oversized dialogs.
5. **Right inspector/dock:** changes, test logs, tool activity, artifacts, model metrics, references, runtime status. Reserve real width; collapse or choose a tab at small sizes. No covering the composer.
6. **Bottom status:** selected project/branch, agent profile, local/cloud identity, active jobs, errors and connectivity. Status values must be measured and avoid redundant badges.
7. **Overlay coordinator:** single modal host, nonblocking anchored popovers, notifications, drawers, native pickers, no hidden click-through or stale focus.

## Route hierarchy

**Frequent:** Home; Chat; Project Agent workbench; Project Files; Changes/Review; Terminal; Tasks/Activity; Models; Skills; Connections; Workflows.
**Contextual/deep:** New project flow, GitHub repo browser, model detail/download, provider credentials, installed skill detail/update diff, workflow editor, image studio, schedule detail, settings categories, diagnostics, onboarding, help/release notes. `source_material/premium/upgrade/data/routes.json` contains 21 proposed routed surfaces with actions, minimum data and states. Validate that inventory against the real app before implementing.

## Core journeys

### A. New user
First launch → clean welcome with `Connect a model`, `Open project`, `Try chat` (only if usable). Connection wizard discovers local endpoints without calling unapproved remote endpoints. A real streaming test clearly states what data will be sent. User lands in chat with model provider prominently labeled. Returning user gets last context and a non-disruptive changelog only after updates.

### B. Quick coding task
Open/reopen project → pick cloud/local profile at composer → describe goal → see scoped plan and requested permissions → stream task/tool timeline → approve meaningful privileged operations → inspect actual changed files and tests in inspector → revise/accept/commit → history remains searchable. Changing tabs must not cancel a run.

### C. Parallel agents
Create task group → select agents, models and isolated worktrees → scheduler displays queue/resources → individual stream views → conflict/integration review → compare results → accept approved changes and merge deliberately. If shared workspace cannot be safely isolated, prevent simultaneous writes with an explanation.

### D. Marketplace
Browse, filter and search → detail page: author/repo/commit/license/platform/dependencies/data access → verified source fetch preview → opt-in install to user-data directory → initially disabled → skill fixture test → enable per workspace → update available with diff → stage/review/rollback. Unsupported or unverified entries must say why.

### E. Model management
Installed/discover/active/compare/downloads tabs → per-runtime endpoint identity → active GPU/RAM estimates distinguished from observations → filter tasks and license → download with progress/checksums → configure local vs cloud routing and fallback rules → switch from composer without leaving task.

## Navigation invariants

- Browser-like back/forward history is for **routes**, not destructive run actions. Restore last focus and list-scroll positions.
- Ctrl+K command palette fuzzy-searches accessible routes, actions, commands, projects, tasks, skills and models with categorized results, shortcuts and safe confirmation on side effects.
- Context menus are on the item, support right-click and keyboard menu key/Shift+F10; use explicit verbs with disabled reasons.
- A modal never appears behind a drawer or another modal. Escape closes only the topmost dismissible overlay; focus returns to its initiating control or a safe fallback.
- Popovers clamp/flip within monitor and client rect, handle DPI and resizes. A hidden overlay has both visibility and hit-testing disabled. No transparent full-screen canvas absorbs clicks by accident.
- Loading placeholders preserve layout. Errors persist in activity and offer next actions. No success toast before a host confirms it.
- Working text draft, attachments and model settings survive route navigation and normal restart; temporary chat is explicitly ephemeral.
- At narrow widths, collapse inspector before hiding essential actions. Settings and review UI remain fully scrollable with single scroll owner per region.
- Agent tools and output live in the active task, not behind Settings. Avoid needless navigation between chat, diff, terminal, tests and approvals.

## Mandatory components & states

Create the native component library with visual/behavior contracts for `AppShell`, `NavRail`, `ProjectSwitcher`, `CommandPalette`, `RouteFrame`, `SecondarySidebar`, `InspectorDock`, `SessionList`, `ConversationTranscript`, `Composer`, `ModelPicker`, `TaskTimeline`, `AgentStatusChip`, `ApprovalCard`, `PermissionDialog`, `FileExplorer`, `DiffViewer`, `TerminalPane`, `TestResultPanel`, `ArtifactPreview`, `WorkflowGraph`, `WorkflowRunView`, `SkillCard`, `SkillSourceInspector`, `SkillUpdateDiff`, `ModelCard`, `ModelDownloadQueue`, `IntegrationHealth`, `ActivityCenter`, `SettingsEditor`, `NotificationToast`, `OnboardingGuide`, `ErrorRecoveryPanel`, and `CompanionHost`. Source premium inventory has 58 component contracts; implement and test the states given there.

**Shared component state matrix:** loading/empty/content/error/offline/disconnected/permission-denied/disabled/busy/cancelling/completed and route disposal where relevant; keyboard focus, high contrast, screen reader name and notification behavior. Baseline screenshot comparisons at 1440×900, 1280×800, 900×700, 760×560 and 560×700; do not claim Linux GUI verification from static linting.

## Exact menu behaviors

- File: Open Folder, Open Recent, Clone Repository, New Chat, New Task, Import/Export Archive, Exit.
- Edit: Undo/Redo applicable edit, Cut/Copy/Paste, Find (contextual), Preferences.
- View: Navigate Back/Forward, Toggle Left/Right Panels, Terminal, Activity, Zoom, Theme, Fullscreen.
- Agents: New Task, Run Profile, Permissions, Agent Status, Suspend/Cancel when supported.
- Tools: Models, Skills, Connections, Workflows, Image Studio.
- Help: Keyboard Shortcuts, Guide, Logs, Diagnostics, Updates, Release Notes, About.

Do not ship inert commands. Native OS menu conventions can differ by platform but parity of intent is mandatory.
