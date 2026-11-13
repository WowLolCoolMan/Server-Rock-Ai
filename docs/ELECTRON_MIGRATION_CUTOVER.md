# Electron replacement cutover

This is the owner decision for the desktop architecture: the Electron + React
renderer and persistent .NET agent host will fully replace the Uno/XAML desktop
shell. The Uno project is legacy compatibility code during the migration and
must not receive new product-only behavior once an equivalent host-backed route
exists.

## Replacement boundary

```text
Electron main process
  ├─ sandboxed React renderer
  ├─ context-isolated preload bridge
  └─ persistent .NET 10 agent host
       └─ ServerRock.Core services, stores, policies, and providers
```

The renderer has no Node.js or direct filesystem access. The main process owns
host lifecycle and request correlation. The host owns local persistence,
provider routing, workspace permissions, task execution, and external
integration policy. Core remains the shared domain boundary while the host
adapts it into versioned application contracts.

## Retirement gates for Uno

The Uno shell may be removed only after each gate has current evidence in the
capability matrix and goal ledger:

1. Chat, model discovery/selection, attachments/RAG, memory, queueing, export,
   search, and settings are host-backed in Electron.
2. Projects, bounded file browser/editor, terminal, Git review, worktrees, and
   undo have equivalent review and safety boundaries.
3. Agent tasks, workflow approvals, scheduling, MCP, skills, and recovery have
   durable host contracts and failure-state acceptance.
4. Accessibility, keyboard/focus, narrow-window, packaged startup, data
   retention, upgrade/rollback, and supported-platform acceptance pass on the
   replacement artifact.
5. Legacy data paths and archive imports are proven readable by the replacement
   before any native project or data cleanup is considered.

Until all gates pass, retaining Uno is intentional and reversible. Deleting it
would create a false completion claim and remove a comparison surface needed to
validate parity.

## Current replacement evidence

- `runtime.health`, `models.list`, `projects.list`, `project.save`, `tasks.list`,
  `tasks.queue`, `tasks.cancel`, `tasks.retry`, `scheduled.list`,
  `conversations.list`, `conversation.save`, `settings.get`, `settings.save`,
  `image.generate`, and `chat.complete` are available through the versioned
  host protocol.
- The Electron renderer has persisted projects, conversations, settings, model
  discovery, chat, Agent Workbench, and a local ComfyUI Image Studio surface.
- The Electron chat restores the newest local conversation and saves both the
  user turn and completed assistant response through the bounded Core store.
- The replacement sidebar now exposes the first eight saved conversations and
  an explicit New chat action; selecting a history item restores its messages.
- Agent Workbench exposes reviewed Queue, Cancel, and Retry actions for the
  safe durable task states; every action refreshes from the host after the
  transition.
- Electron Projects can open a bounded project-file browser. It shows only
  relative paths and byte counts, reports protected/dependency/link skips, and
  never returns file contents to the renderer.
- Selecting a listed text file opens a bounded, safety-checked preview through
  the host; the renderer still receives no filesystem handle or direct path
  access.
- Electron Projects also exposes an explicit Terminal route. Commands run only
  after a user action inside a selected project root, with bounded command,
  output, working-directory, environment, and timeout controls.
- `npm run smoke:agent-host` exercises the replacement protocol against
  disposable settings and conversation files, including persistence round trips.
- `apps/desktop` typecheck and production build pass.
- The host builds and a live stdio request returns the new capability set.
- The repository root now defaults to Electron for `start`, `build`, and
  `open`; explicit `*:legacy-native` aliases preserve the Uno rollback path.
- `npm run audit:electron-runtime` verifies the default commands and the
  Electron sandbox/context-isolation/preload boundary.
- A live disposable host smoke verifies `project.files` filtering and the
  existing 128-entry browser limit.
- `npm run smoke:project-browser` verifies project persistence, exclusion
  reporting, and the metadata-only browser contract against disposable paths.
- `npm run smoke:project-terminal` verifies the disposable project command
  round trip and bounded output result.

The replacement is not complete. The remaining work is tracked in
`docs/HANDOFF_BACKLOG.md`, `docs/CAPABILITY_MATRIX.md`, and
`docs/GOAL_EXECUTION.md`.

## Rollback

The replacement shell can be rolled back to the Uno compatibility surface while
host contracts are corrected. No user data is migrated or deleted by this
architecture decision.
