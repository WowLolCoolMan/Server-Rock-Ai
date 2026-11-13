# Full screen blueprints — build each to functional completion

Each screen must implement: route + visible content + meaningful empty/loading/error/offline/permission states + keyboard path + persistence behavior + tests. This document is an interaction specification, not a demand to rewrite all screens at once.

## 01 Home / command dashboard
**Zones:** greeting/project resume (top), task composer (center), active/awaiting approvals (small, prominent), pinned/recent projects, recent conversations, meaningful notifications. **Primary CTA:** New task. **Secondary:** Open folder, New chat, Run workflow, Connect model. **Empty:** first-run call to select a model; 1–2 clear onboarding actions; no filler cards. **Continuity:** opening task focuses existing project, never creates duplicate by accident. **Keyboard:** input focused optionally on startup; enter-to-send and multiline behavior configurable.

## 02 Global Chat
**Zones:** searchable folder tree, active thread, response/artifact column, composer with model/profile, attachment chips, local/remote status and tools. **Actions:** new, rename, archive, pin, search-in-chat, fork, edit/resend, retry, copy/export, stop. **Message design:** content-width cap, robust Markdown/code/table, source cards and tool trace collapsed by default, line wrapping, syntax highlight and accessible copy. **State:** streaming, queued, failed, stopped, done; visible privacy/attachment route before remote send. **Persistence:** active thread and scroll anchor preserved when returning; temporary chat explicitly ephemeral. **Failure:** stream drops show partial response + retry; do not duplicate model turns.

## 03 Project Overview
**Zones:** project title/root and Git badge; open tasks, recent files, branch/worktrees, project instructions, model/profile defaults, recent commits, health report. **Actions:** open folder, edit root, new task, manage context, open Git, quick file search. **Safety:** exact roots with trust state; symlink external boundary not traversed silently. **Empty:** clean CTA for a folder or repo; missing path offers relink, not silent creation.

## 04 Coding Workspace / Workbench
**Layout:** left explorer/task list, center flexible Chat/Code/Split tabs, right inspector (Diff/Context/Run/Tests/Approvals), optional bottom terminal dock. **Actions:** select project/branch/worktree, run agent, stop/pause, approve tool step, preview proposed file write, run tests, stage/commit after review. **Session:** model and task controls near composer, never forcing users to navigate to Model settings. **Recovery:** external file changes and conflicts produce diff with 'keep mine / keep theirs / open compare' after safe checks; never overwrite quietly. **Keyboard:** tabs, find, collapse panes, focus terminal/editor. **Empty:** explain select workspace or attach existing branch.

## 05 Editor & File Browser
Virtualized hierarchy with filter/favorites, ignored/secret markers, binary/unsupported reasons, tabs, dirty indicators, close confirmation, contextual path menu. Native code editor must support selection/indent/navigation/find/go-to-line/highlight appropriate to chosen engine; do not claim full IDE parity without implemented functions. Quick open by filename; safe preview for untrusted content. External modifications and encoding/newline preservation are testable. Secrets excluded from agent context by default.

## 06 Diff / Review Center
**Input:** proposed change set, baseline hash, allowed workspace root, explanation, touched files, side effects, associated task. **Render:** unified or split diff, per-file/per-hunk where implementation supports, additions/deletions, binary files, context lines, jump navigation, comment, tests. **Actions:** Approve once, Reject, Modify scope, Retry generation, Save checkpoint. **Rule:** preview != applied != committed. **Conflict:** stale baseline blocks writes and offers rebase/reinspect. **No hidden apply:** closing dialog is not consent.

## 07 Terminal / Output / Problems dock
Persistent tab per project/task, true OS PTY with resize, scrollback, command history, copy, keyboard, search, environment and current working directory display. Commands proposed by agents require scoped review; user-typed terminal is distinct from agent tool execution. Start/stop/restart progress visible. Crash, executable missing, bad cwd, permission failure and process tree cancellation all have recovery. No credential echoes; handle ANSI safely.

## 08 Agent Center
**Views:** lanes (running, queued, waiting, completed), task list, detailed run timeline, approval inbox. **Card:** project, agent identity, model, cost/usage if available, state, elapsed time only as observed, branch/worktree, last activity. **Actions:** create, duplicate, pause (when supported), cancel, resume/retry from safe checkpoint, review outputs. **Multi-agent:** isolate write scopes; child tasks attached to parent; graph shows dependencies; no claim of parallelism for serialized work.

## 09 Workflow Library & Visual Builder
**Library:** saved templates grouped by design/review/test/docs/research; search, favorite, clone, import/export with sanitization. **Builder:** typed blocks (Agent, Tool, Condition, Parallel, Approval, Test, Notification), ports/edges, inline schema validation, minimap, zoom, undo, keyboard accessibility, run simulation. **Run screen:** logs, step completion, branch reasons, failures, pause, retry only idempotent nodes. **Version:** changes do not mutate running version; prompts/tool config redacted from exports as configured. **Triggers:** schedule/remote triggers only if explicitly implemented, permitted, and visible.

## 10 Model Library / Router
**Tabs:** Installed, Discover, Downloads, Providers, Profiles. Cards indicate provider, task modality, parameters/context if verified, disk footprint, estimated compatibility flagged as estimate vs measured benchmarks. Download/pull/delete includes source/license/cost/disk confirmation and resumable progress where possible. Show Ollama/LM Studio/local hosts distinctly by host+model identity; never confuse cloud route with local. Auto/Cloud/Local profiles and fallback reason must be inspectable. GPU saturation gracefully queues or declines.

## 11 Skill Marketplace
**Tabs:** Discover, Installed, Updates, Sources, Permissions. Catalog search facets (category, provider, task, OS, trust, version). Detail: SKILL.md synopsis, upstream source, pinned ref/hash, author/license (verified), expected tools/files/hosts, dependencies, permissions, usage, examples, reviews only if real verified data exists. Lifecycle: staged install → validate → permission review → activate; enable per project/session; update compare; rollback; remove with owned-files-only confirmation. State labels: candidate, verified, installed, enabled, selected and invoked are different.

## 12 MCP / Connections Hub
**Groups:** model providers, coding agents, MCP tools, GitHub, ComfyUI, local runtimes. Provider tile shows true connection state (unconfigured, connecting, healthy, degraded, authorization failed). Detail pane: scope, tool inventory, permissions, account identity where legitimately known, endpoint, health test, logs, reconnect/revoke. No surprise redirect to settings. Never display full secrets; support environment variable references and OS keyring.

## 13 Git Workspace / Source Control
**Zones:** branch/worktree selector, changed/staged files, diff, commits, remote status, conflicts. **Actions:** inspect status, stage subset, compare, commit with reviewed message, push with explicit confirmation, view remote. **Recovery:** detached head, missing git, auth expiry, network timeout, nonfastforward and merge conflict. **Multi-agent:** worktree labels and owner tasks. Never stage secrets or unreviewed changes silently.

## 14 Activity / Inbox / Download Manager
Unified feed of actionable approvals, run completions, failures, downloads, install updates and warnings with filter, grouping, unread state, jump-to-origin and diagnostics. Background tasks do not vanish when user leaves page. Toasts are for transient events; inbox is for lasting actionable items. Retry buttons show exact operation and avoid duplicate effects.

## 15 Settings / Preferences
Searchable vertical sections: General, Appearance, Chat & Composer, Projects, Agents & Runtime, Models & Providers, Tools/Skills/MCP, Permissions, Privacy/Storage, Notifications, Updates, Diagnostics/About. Each control shows current effective scope (global/workspace/session) and whether restart required. Layout previews; export/import settings without secrets; 'Open Server Rock data folder'; high-risk Reset asks exactly what is removed.

## 16 First-run / onboarding
3–5 purposeful steps, skip/return possible: select local folder, detect local model runtimes, choose Local/Cloud/Auto, review privacy/permissions, begin chat or project. Detect absent model and propose safe setup; never demand cloud signup. Don't infer provider authorization from binary discovery. On Linux show missing keyring/GTK dependencies with actionable diagnostics.

## 17 Diagnostics / Recovery
Human-readable app/core version, OS/runtime, model connection status, startup errors, storage locations, log filters, sanitized bug export, crash recovery. Safe repair actions are scoped; never 'clean repo' or delete model folders. Signed update and rollback status visible. Explain what logs collect and require user consent before uploading anything.

## 18 Companion / optional personal touches
Existing desktop pets must never capture terminal input, obscure confirmation buttons, intercept clicks, degrade performance, or surprise users. Toggle clearly, per-workspace preference and Linux/window manager support as verified. This is optional polish, never a blocker for core task execution.

## Common state checklist for every screen
Loading / empty / content / disconnected / offline / partial data / no permission / invalid input / retry pending / cancellation / timeout / stale data / 2nd window / restored after restart / high DPI / narrow viewport / screen reader. See `INTERACTION_STATE_MATRIX.md` and generated JSON route catalog.
