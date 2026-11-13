# Full product navigation and specific screen designs

## Navigation graph

`Home` (optional dashboard) → `Chat` → `Agent Runs` → `Projects` → `Models` → `Skills Marketplace` → `Images` → `Tasks` → `Settings` → `Diagnostics`. These are **routes**, not popup dialogs. Each route owns one view model and has stable back navigation. Deep links may select a run/model/skill/project. Preserve per-route scroll and filters for that session. Workspace should persist across route transitions but not auto-switch project context unexpectedly.

### Chat

Layout: left saved conversations + search; center virtualized timeline; optional right context inspector; composer at bottom. Header: chat title, active model badge, local/cloud privacy indicator, tools enabled, selected attachments. Composer: model picker, attach, web toggle, skill suggestion and send/stop. Expand controls only on demand. Show source cards and tool evidence per assistant message; mark tool output separately from assistant prose. On hover/focus expose Copy, Edit, Retry, Branch, Export. If user scrolls up, stop automatic scrolling; show “Jump to latest”. Draft persists per conversation while user navigates elsewhere.

### Agent

Header: project selector, run label, autonomy policy, current model. Main: concise goal, plan timeline with pending/running/blocked/tested states. Right inspector tabs `Files`, `Diff`, `Tests`, `Permissions`, `Context`. Bottom collapsible tool log with copy and filter. Explicit actions: Start, Pause, Resume, Cancel, View changes, Commit. A run must not vanish when navigating away. A completion claims verified only when test evidence exists. Failed step shows error and actionable retry. Approval panel shows exact action, affected paths, origin and once-only scope; suspend run without blocking all other pages.

### Projects

Two-pane: project list/search and details. Project detail includes root folders, Git status, branch, dependency inventory, indexing health, last runs, permissions. File explorer must be lazy and allow search by names/symbols; no 16-file approximation presented as complete coverage. Files open in inspector or tab, with edit/undo/conflict UI. Git commit review enumerates exactly included files/hunks and excludes unselected staged changes.

### Models (Supermarket)

Page header: installed/downloadable tabs and runtime status. Left filter: provider, capability (text/vision/tools/embedding/audio), memory fit, license, quantization, estimated size, local/cloud, model family. Main searchable list with virtualization; details pane: runtime compatibility, context window, required backend, VRAM/RAM estimate, download size, license, source commit/revision, measured TTFT and tok/s. Actions: Download/Install, Load, Use, Benchmark, Unload/Delete; model-specific confirmation for deletion. Downloads show resumable progress and error log. Cannot select repository metadata as if it is loaded/runnable.

### Skills Marketplace

Header: Installed, Discover, Updates, Drafts, History. Search and tag filters. Cards display name, description, verified source, version, permissions, model/OS compatibility, last checked, update badge. Details include `SKILL.md` rendered safely, file tree, origin repo+commit, authors/license, triggers, capabilities, test audit, version diff. Install opens review with exact effective permissions. Update center shows changed instructions/permissions and rollback. “Use skill” attaches to active agent plan and reports why it was selected; not only a prompt-copy button. Users can also ask agent to discover/create relevant skills, but approval is required for publication and executable rights.

### Images / ComfyUI

Three-pane: workflows (left), generation queue and image results (center), selected workflow/settings (right). Prompt composer includes model/workflow, seed, resolution, steps, sampler with validation. Add a workflow importer with compatibility/required models/custom nodes preflight. Queue rows show queued/running/completed/failed/canceled, result images open in viewer with metadata. Work continues while user visits other pages; cancel exactly matching ComfyUI job and do not interrupt unrelated user jobs.

### Tasks

List with Upcoming, Active, History, Failed. Creation form: prompt, specific date/time or recurrence, timezone, model and autonomy policy, target workspace, tool permission set, notification. Each task shows last run, next run, state, pause toggle and run log. Execution persisted beyond UI page lifecycle. Scheduler shows transparent “app must be running” if no background service installed.

### Settings

Full route, category sidebar + searchable content. Sections: General/Appearance, Models & Providers, API Keys, Privacy & Data, Agent & Permissions, Tools & MCP, GitHub, Updates, Accessibility, Notifications, Diagnostics. Change fields in draft state; Save/Cancel in persistent footer when dirty. Validate inline before closing; if an external model connection fails, allow user to stay and correct it. Separate connection test from save and redact secrets. Avoid nesting modal model picker in Settings.

### Diagnostics

Health cards for provider connectivity, model runtimes, Git/SDK, plugin/MCP connections, tasks, storage, release version. Export redacted logs with preview; background work and failure history. “Run diagnostics” captures evidence without changing external systems.

## Navigation state contract

`ShellState = {currentRoute, backStack, projectId, selectedChatId, selectedRunId, leftPaneMode, inspectorOpen, userTheme, activeOverlays}`. Route VMs own page-specific filters/drafts/scroll, not global `MainPage` fields. On navigation: serialize draft, cancel only page-local observation subscriptions, keep global run alive, close transient tooltips/popovers, retain notification/history, restore focus to new page heading. No route may spawn a project selection dialog automatically just because mode changed.
