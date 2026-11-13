# Information architecture, routes, menus, workspace continuity

## Persistent desktop shell
```
Window chrome/titlebar:  Server Rock AI | workspace picker | global search/command | model/runtime health | inbox | account/settings
Left rail (48–60):      Home, Chat, Projects, Agent Center, Workflows, Library, Activity
Secondary sidebar:     context-sensitive tree (chats/folders, projects/branches, runs, models/skills)
Center workspace:      selected task/conversation/editor/workflow/library (tabs and splits)
Right inspector:        Context, changes, approvals, sources, tests, logs, collaboration state
Bottom status:          OS, git branch, workspace, provider, route, context budget, background activity
Optional bottom dock:   Terminal, Problems, Output, Tasks (resizable; avoid a pop-up terminal by default)
```
No full-page navigation for every small feature. Model picker, search, skills usable in composer and command palette; dedicated library pages still exist for management. Keep the current native app's separate ChatBot/Coding Agent modes functioning during transition; build shared navigation state incrementally.

## Recommended top-level visible items
- **Home:** recent project/chat, active tasks, quick start, missing runtime connection.
- **Chats:** global conversation list, folders, pinned, search. New conversation stays simple.
- **Projects:** projects and branches; within project: Overview, Tasks, Code, Git, Conversations and Notes.
- **Agents:** queue, parallel running lanes, approvals, task history, live statuses.
- **Workflows:** gallery, versioned editor, runs, failures, templates.
- **Library:** tabs for Models, Skills, Integrations/MCP; avoid three equally heavy sidebar destinations when space constrained.
- **Activity:** notifications, logs, background jobs, downloads, recent changes.
- **Settings:** gear in shell/footer, searchable sections and platform-specific diagnostics.

## Route/deep-link model
Canonical examples (virtual app routes; not a web server):
- `home`
- `chats/list?folder={id}`; `chats/{conversationId}`
- `projects`; `projects/{projectId}/overview`; `projects/{projectId}/tasks/{taskId}`
- `projects/{projectId}/code?file={relativePath}`; `projects/{projectId}/git`
- `agents/queue`; `agents/{runId}`; `agents/approvals`
- `workflows/library`; `workflows/{workflowId}/edit`; `workflows/runs/{runId}`
- `library/models`; `library/skills`; `library/integrations`
- `activity`; `settings/{section}`
Persist last active workspace, selected conversation, open tabs, panel geometry, active context and scroll positions per workspace. Do not put secrets, absolute paths or prompt contents in exposed navigation identifiers.

## Navigation contract
- Back/Forward behaves across app routes; navigation history persists for current process. Reopening restores the last safe route or falls back to Home if resource missing.
- Global search/command palette `Ctrl+K` searches actions, projects, chats, files, models, skills and settings with clear labels/icons and shortcut hints. It must not execute destructive commands on highlight/Enter without approval.
- `Ctrl+P` quick-open files in a project; `Ctrl+Shift+P` command mode; `Ctrl+,` settings; `Ctrl+Tab` switch open tabs; `Esc` close topmost transient overlay.
- Context menu on project/chat/file/run supports rename, pin, duplicate (where meaningful), archive, copy link/path, reveal, and delete with confirmation and undo when feasible. No menu item without a handler.
- Clicking a push/commit/update/permission entry leads to an **in-context dialog**, not an unexplained Settings redirect. Back/Cancel restores prior route, focus and scroll.
- Unavailable pages use honest explanation, prerequisites, and one action that resolves the missing condition. No blank page or generic broken button.

## Panel policy
- All panes are resizable with min/max constraints. Collapse gracefully below compact breakpoint. Save layout by workspace and per-window context.
- Narrow window: collapse side tree to drawer; show inspector on demand; keep chat composer visible; terminal becomes switchable dock tab.
- App does not hide errors/progress behind modal windows; background tasks can run but their state stays visible in Activity.
- Primary action appears near relevant object. Advanced options appear in `...` menu or inspector, not as dozens of toolbar icons.
- Left rail may display unread/approval counts (not statuses already visible elsewhere).

## Native menu map
File: New chat, New project, Open folder, Recent, Import/Export, Exit.
Edit: Undo/Redo (contextual), Copy, Paste, Find, Preferences.
View: Toggle sidebar/inspector/terminal, Zoom, Compact density, Fullscreen, Focus mode, Restore layout.
Agents: New task, Pause/cancel, Approval inbox, Agent settings.
Tools: Model Library, Skill Marketplace, MCP Connections, Diagnostics.
Help: Shortcuts, Docs, Report issue, Release notes, Check updates, About.
Menu items must use OS conventions and work in Windows/Linux; verify accelerators and focus.
