# Skai Premium UI Transformation

This ledger records the evidence-backed state of the active frontend migration. It is intentionally honest about partial and unverified work.

## Current slice — Explicit local task execution

The Workbench now exposes a real `Run locally` action for queued tasks. The
Electron bridge forwards the request through the isolated IPC allowlist to the
Agent Host, which claims the durable task, sends the task instruction to the
configured local model endpoint, and persists a completed or failed result.
The first execution boundary is intentionally narrow: this slice performs
local model reasoning only and does not claim file edits, shell commands,
external tools, cloud execution, or approval-gated actions.

The UI reports the queued state as ready for local execution and retains the
failure summary when the local provider is unavailable. This turns the task
record from a planning-only surface into an honest executable local workflow
while leaving tool/file mutation for a separately verified approval slice.

Agent Host startup now reconciles tasks left in `running` state after a
process interruption into an explicit blocked state. It never silently retries
the model or releases a retained worktree; the user must review and make a
new retry decision.

Coding Workspace now reads the selected project's local Git branch, changed
paths, ahead/behind counts, and conflict state through a dedicated host method.
The UI exposes refreshable status and a bounded change list, while clearly
labeling stage/commit/push as not yet connected.

The same review surface now loads a bounded local diff and supports an explicit
local commit after the user enters a message. Protected paths and conflicts
remain fail-closed; push and pull-request work are still separate because they
need explicit remote-state and credential review.

## Current slice — Packaged appearance acceptance

The packaged Electron acceptance harness now drives the real Appearance
controls for dark, light, and system theme modes plus Deep Sea and Deep Space
backdrops. It checks the resulting semantic theme tokens, verifies no
horizontal overflow at normal and constrained widths, and captures each state
under `.tmp/theme-acceptance/` for visual review.

## Current slice — Utility capability audit

The remaining secondary workspaces were audited against their actual local
handlers. Model discovery, OS voice preview, ComfyUI generation, project
terminal execution, task transitions, memory editing/export, and settings
save all retain real handlers. Marketplace categories do not expose fabricated
install or activation controls; unavailable catalogs are labeled as such.
The Models surface now also provides a one-action recovery when a valid local
model inventory has no search match.

## Current slice — Workbench artifact continuity

Agent task artifact labels already persisted by the local task store now travel
through the Agent Host task-list response and appear in the Workbench detail
disclosure. Tasks without recorded artifacts say so explicitly; no artifact is
invented by the renderer.

The same labels are now echoed in the compact Coding Workspace activity rail,
so a user can see completed outputs without leaving the project session.

Open editor tabs are now restored per local project and capped to a bounded
list. Switching projects no longer destroys the user's working set, while file
contents are still re-read through the guarded project-file handler when a tab
is opened.

The editor's Undo action now reflects the actual local undo receipt. It is
disabled, with an explanatory title, until a saved edit provides a reversible
operation.

The active project editor now also handles Ctrl/Cmd+S through the same guarded
save operation and advertises the shortcut beside the existing file and
terminal shortcuts.

Open-file tabs now use roving focus with ArrowLeft/ArrowRight and Home/End,
retain a single active tab stop, and expose the editor relationship through
`aria-controls`.

Local model discovery now preserves its real asynchronous result through the
workspace action. The Models surface disables duplicate requests and reports
`Discovering…` until the local provider responds.

Settings uses the same guard and progress label for its model-provider panel,
so both entry points behave consistently.

The Models surface now keeps the last known inventory visible after a failed
refresh, marks the registry `Needs attention`, and explains that the previous
inventory is being shown instead of presenting stale data as ready.

Discovery status is announced through the shared status region and the active
button exposes `aria-busy`, so keyboard and assistive-technology users receive
the same progress and recovery signal as visual users.

Task environment radios now use an instance-scoped group name and describe the
local-only execution boundary directly from the control, avoiding collisions
when more than one composer is mounted.

Scheduled local runs now validate their date before submission and show a
recoverable error when the host rejects a schedule instead of failing silently.

## Current slice — Menu exit behavior

Overflow actions and recent-chat context menus now close and restore focus to
their trigger when the user presses Tab. Escape and arrow navigation remain
available, and menus no longer remain visually open after keyboard focus has
moved away.

## Current slice — Model picker focus discipline

The local model picker now keeps Tab focus inside its open popover, updates the
highlight when an option receives focus, and exposes the active option through
the search combobox relationship. Escape, arrow navigation, selection, and
outside-click behavior remain intact.

## Current slice — Composer command-menu accessibility

The slash-command menu now exposes its highlighted option through the composer
textarea’s listbox relationship, keeps the active option scrolled into view,
and supports wrapped ArrowUp/ArrowDown plus Home/End navigation. Commands
remain local and use the same selection behavior as before.

## Current slice — Honest integration readiness

Settings no longer presents GitHub as connected in the Electron surface when no
GitHub bridge or sign-in flow is exposed there. GitHub is marked as needing
setup, while local model, voice, and image services are marked available only
when the selected local route supports them. The shared settings shell remains
functional without adding a fake connection action.

## Current slice — Scheduled work state clarity

Scheduled local tasks now present semantic status badges, readable due times,
recurrence labels, and the workspace that will receive the task. Completed,
failed, paused, running, cancelled, and unknown states remain distinguishable
without exposing raw backend status tokens. Existing pause, resume, cancel, and
explicit “Mark due” behavior is unchanged; the UI still explains that
background execution is not enabled.

## Current slice — Desktop-wide Percy drag reliability

Percy remains a separate transparent Electron window rather than an element
inside the Skai workspace. During a drag, Electron now follows the desktop
cursor from the main process while the renderer is outside the companion
window, then clamps the window to the active monitor work area and persists the
final position. The companion is also marked visible on all workspaces when
the platform supports that Electron capability. Packaged smoke now verifies
that the Percy page is a distinct transparent animated surface from the main
renderer.

## Current slice — Failed-task replanning handoff

Workbench’s failed-task recovery now carries the bounded task brief and prior
result into the local Chat planner. The handoff opens the planner, keeps Local
selected, and removes its one-shot storage payload after consumption. A
packaged Electron smoke test exercises the route change and rendered values;
the host still honestly reports failed-task retry as unavailable.

## Current slice — Bounded recent-chat navigation

The desktop sidebar now gives recent conversations their own bounded scroll
region. The header, workspace links, utility links, search field, and Settings
action stay stable while longer histories scroll inside the sidebar instead of
overflowing the window. The packaged Electron build was visually inspected at
desktop size after the change.

## Current slice — Marketplace catalog density

Marketplace category cards now fit in the normal desktop view beside their
detail pane, improving discoverability without implying that unavailable
local catalogs can be installed. Search, filters, selected detail, and local
readiness messaging remain intact.

## Current slice — Image Studio prompt-first workflow

The image workspace now puts endpoint, prompt, and starter recipes in the
first-use path. ComfyUI workflow JSON is still visible as an explicit advanced
section and remains required before generation; the change only improves
information hierarchy and available desktop space.

## Current slice — Workbench empty-state behavior

Agent Workbench empty task/detail states now have explicit layout behavior,
so an empty filter presents its message without a needless nested scrollbar.
Real task and scheduled-run histories remain bounded and scrollable when
content requires it. The change preserves local-only execution semantics and
the existing list/detail, scheduling, and recovery actions.

## Current slice — Chat and Coding composition

Chat now uses a fluid conversation canvas with semantic message rows rather
than a pale oversized card. User messages are visually secondary, assistant
content remains easy to scan, and the composer is a contained sticky surface
at the end of the conversation flow. Coding’s empty-project overview now
uses the available workspace height for its project and workflow surfaces,
reducing the dashboard-like dead space while preserving the local project
creation and handoff actions.

The slice was typechecked, production-built, packaged, smoke-tested, and
inspected in the running Electron app. The app remains local-only; no cloud
route or hosted provider behavior was introduced. The overall premium rebuild
continues.

## Current checkpoint

Percy’s separate transparent companion now keeps completion feedback truthful
even when a local request cannot finish: host-unavailable, timeout, process-exit,
and write-failure paths produce a brief error bubble and error pose. Its live
status is announced accessibly while the six-frame sprite remains independently
draggable and outside the main app window.

Renderer routing now has a typed history state shared by the app shell and
toolbar. Back/Forward controls are disabled when no browser-style entry is
available, duplicate route selections do not add history noise, and Electron
uses reload-safe `index.html#/route` URLs while browser/dev builds keep
pathname routes. Live Electron navigation and persisted route restoration were
verified; packaged verification is recorded in the execution ledger after the
final audit pass.

## Completed checkpoints

- Current checkpoint — Percy’s standalone desktop companion now reports both
  successful and failed local work with short bubbles and matching six-frame
  success/error sprite states. The transparent always-on-top window remains
  independently draggable across monitor work areas and preserves its position;
  packaged smoke and live Electron inspection confirmed the separate surface.

- Current checkpoint — Models, Image Studio, and Marketplace now have
  independent local-catalog feature modules. Their search/filter/readiness
  states remain aligned with the shared shell while each route can evolve
  without a cross-page monolith.

- Current checkpoint — Coding Workspace now separates its page shell from the
  active local session. Project switching, file search/tabs, editor/diff review,
  task planning, terminal docking, local persistence, and activity handoff are
  contained in `CodingSessionView`, while the route shell owns the project
  overview and create-project dialog.

- Current checkpoint — Agent Workbench presentation is now split into focused
  task list, board, detail/action, scheduled-run, and shared presentation
  modules. Existing filters, summary cards, next-step guidance, keyboard
  navigation, recovery handoffs, and local-only scheduling semantics remain
  intact while the route becomes easier to extend without another monolith.

- Current checkpoint — Coding Workspace now includes a live task-activity rail
  beside the project/file surface, showing real local task counts and recent
  results with a direct Agent Workbench handoff. The empty state remains
  project-first and no unsupported agent execution is implied.

- Current checkpoint — Chat’s composer is now an independent reusable feature
  with a visible drag-and-drop attachment state. Dropped files use the same
  guarded local validation and PDF extraction boundary as picker attachments;
  no renderer filesystem access or cloud route was added.

- Current checkpoint — Chat’s composer now has a keyboard-first local slash
  command menu with filtering, pointer and ArrowUp/ArrowDown selection,
  Escape dismissal, local route handoffs, and a review-first `/goal` task
  planner command. It is bounded to existing local handlers and does not add
  cloud behavior.

- Current checkpoint — Chat’s model control is now a reusable local picker with
  provider/capability identity, search, keyboard navigation, selected state,
  Escape dismissal, and an honest empty-registry handoff to Models.

- Current checkpoint — Percy’s separate transparent companion window now has
  safer drag lifecycle handling, monitor-aware native clamping, reliable hide
  control behavior, and a retry completion summary while retaining the bundled
  six-frame walk/idle/success sprite sheets.

- Current checkpoint — fixed packaged Electron smoke target selection so the release gate verifies the main Skai window and separate Percy companion independently; portable Windows artifact packaging and smoke validation passed.

- Current checkpoint — shared sidebar routes now expose semantic `aria-current="page"` state alongside their visual active styling; the rebuilt Chat shell was inspected live in Electron.

- Current checkpoint — standardized high-frequency navigation, sidebar, Models, command-palette, companion, and attachment controls on the shared typed SVG icon system; live Electron verification confirmed the refreshed Chat and Models surfaces.

- `c5e5276` — persisted the last saved local appearance in renderer startup state so theme, accent, density, backdrop, and reduced-motion settings no longer flash back to defaults while the host loads.
- `20826c8` — added a reusable keyboard-accessible Chat overflow menu to keep contextual actions discoverable without crowding the conversation toolbar.
- `d592412` — added per-conversation local scroll restoration so switching threads preserves the user’s place while retaining automatic scroll for new messages.
- `0d0e105` — added local Memory scope filters and explicit enabled/provenance status while preserving the existing search, edit, disable, remove, and export contracts.
- `a661c25` — added a state-aware Workbench next-step panel so selected tasks explain the actionable local recovery path without inventing unsupported host controls.
- `95a10fb` — rebuilt the Electron workspace visual system.
- `3a40cce` — delivered the premium UI foundation and shared shell primitives.
- `10639d9` — reconnected premium Chat and Coding workflows.
- `58a491b` — completed core workspace feature surfaces.
- `5c67bbf` — added guarded coding workspace editor behavior.
- `62322e7` — added persistent shell interactions and command palette wiring.
- `d24deb3` — added searchable Settings categories, model discovery filtering, and Image Studio history/gallery rendering.
- `f8e90c3` — upgraded Chat with safe markdown/code rendering, message copy/fork actions, contextual conversation actions, persistent drafts, and a bounded follow-up queue.
- `a106d84` — connected the selected coding project to Terminal and Agent Workbench handoffs, preserving project context when opening Terminal.
- `7cfcd82` — added a local, bounded coding diff-review mode tied to the existing guarded file save and undo flow.
- `e95ed81` — deepened Memory and Agent Workbench with loading/error recovery, full task lifecycle columns, dependency context, timestamps, and responsive reflow.
- `e68bde5` — added keyboard-accessible Workbench task-detail disclosures with dependency/result context and honest approval/failure capability notes.
- `373f56e` — added roving keyboard selection, semantic combobox/listbox state, focus-on-open, and focus restoration after close in the command palette.
- `325af1d` — preserved active theme contrast while adding restrained Deep Sea and Deep Space backdrop atmosphere.
- `7313d5c` — added a reusable modal dialog primitive with focus trapping/restoration and migrated the Coding Workspace new-project flow.
- `d635db8` — migrated the command palette onto the shared modal primitive and removed duplicate focus lifecycle logic.
- `6e6eab9` — upgraded Marketplace from static cards to a searchable, filterable catalog shell with selectable detail context and honest unavailable states.
- `ccd20a5` — extracted typed route composition from `App.tsx` into an independently testable `AppRouteView` boundary while preserving the existing feature/service contracts.
- `890e42d` — moved renderer state, persistence effects, backend commands, and feature callbacks into `useDesktopWorkspace`; `App.tsx` is now shell composition only.
- `c3adcc7` — isolated chat conversations, queueing, attachments/PDF extraction, fork/export, metadata, and send/complete commands in `useChatWorkspace`.
- `559f642` — isolated coding/project discovery, folder selection, guarded file editing/undo, and bounded terminal commands in `useCodingWorkspace`.
- `db4b966` — added a local-only task brief composer, truthful Local/Worktree environment state, and a review-first `tasks.plan` Agent Host capability; Cloud is intentionally out of scope.
- `2175eef` — added keyboard-first workspace navigation for command palette, new chat, Settings, sidebar, Chat, Coding, and Terminal shortcuts.
- `b2b39cb` — aligned the visible navigation with the local-first product contract by removing Marketplace from the active route surface while retaining its catalog code for future local extensions.
- `947fb93` — restored Marketplace as a first-class local capability catalog for LLMs, voices/TTS, image models, skills, and tools/integrations with permission-aware copy.
- `9de3357` — exposed the persisted task brief in Agent Workbench details so proposed work can be reviewed before queueing.
- `f1dcf2a` — added backend-confirmed Settings save feedback that keeps the user on the Settings surface after persistence.
- `3c5258e` — added a Chat task brief composer using the existing local-only planning contract and Workbench handoff.
- `3824ada` — fixed Recent Chats to select conversations by persistent ID and restore the selected message set in place.
- `fb2d479` — replaced Chat's oversized page hero with a compact contextual bar and separated the conversation and composer into a stable two-row workspace.
- `4a4d4bb` — made Agent Workbench list/detail the default task view while retaining the lifecycle board as an explicit alternate view.
- `8fb1a13` — tightened the shared PageHeader typography and spacing so secondary workspaces use a compact desktop hierarchy instead of oversized hero treatment.
- `fd9f3f1` — added backend-confirmed Memory save feedback and an explicit confirmation step before destructive removal.
- `d162457` — replaced Terminal's misleading no-project controls with an honest empty state and a real Coding Workspace handoff.
- `442a3e4` — refined Image Studio into a local ComfyUI workflow surface with readiness validation, prompt recipes, accessible field guidance, and a clear generation-state treatment.
- `3e49a33` — clarified Model Library provider identity, vision/text capability, and discovered status while preserving the existing model-selection handoff.
- `52808f6` — added the existing transparent Skai wolf icon to the shell brand lockup and wired the tracked asset pack through the renderer build without adding decorative artwork to dense workflows.
- `f3c9601` — added a branded Chat first-run state and changed the wolf asset reference to a Vite-bundled resource so packaged Electron history routes do not depend on public-root URL resolution.
- `b771eaa` — replaced sidebar Unicode glyphs with a reusable accessible SVG navigation icon set for workspace routes, utilities, and Settings.
- `e676e05` — added a local conversation context menu with stable-ID Open, Rename, Pin/Unpin, and Archive actions, plus compact desktop action spacing.
- `d9fe241` — added a host-backed Chat Retry action that replaces only the selected assistant response, preserves custom conversation titles, and restores the prior response on failure.
- `801f9a1` — persisted the local Coding Workspace terminal dock per project, added a keyboard toggle (`Ctrl/Cmd+J`), and exposed the dock state through `aria-pressed`.
- `bc1364d` — persisted Agent Workbench list/board selection and selected task identity locally, with Arrow/Home/End keyboard movement across task rows.
- `3f38415` — hardened Electron agent-host supervision with bounded pending requests, host-exit cleanup, and Windows process-tree shutdown, with regression assertions in the Electron audit.
- `1c27fae` — added the shared strong-border token used by accessible context menus and compact desktop surfaces.
- `3e4dd54` — exposed the Agent Host's existing local scheduled create, pause, resume, cancel, and run-now operations through Electron's secure allowlist, with Percy completion summaries and audit coverage. The disposable Agent Host lifecycle smoke passed all scheduled transitions.
- `c583744` — renamed Workbench's `Run now` action to `Mark due`, added the persisted outcome line, and changed Percy copy to describe the host's actual behavior—the local store marks a schedule due but does not execute prompts in the background. Renderer checks passed schedule rows and action semantics with no errors or horizontal overflow at 1440px and 960px; screenshot reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-scheduled-after-1440.png`.
- `6cf8b5e` — added a keyboard- and pointer-resizable sidebar with bounded, persisted width so the shared shell adapts to real desktop workspaces without changing route layouts.
- `719b215` — converted the selected-project Coding Workspace into a focused session layout with active-project switching and a single continuous surface for task planning, files, editor/diff, terminal, and workbench handoff.
- `117149b` — added data-derived Agent Workbench activity summaries and keyboard-accessible task filters for active, attention-needed, completed, and all task views.
- `26a84d0` — expanded Settings search across category labels, descriptions, and real controls, with automatic selection of the first matching settings panel.
- `55f231a` — tightened Chat density and contextual rail behavior so active threads stay focused while empty Chat retains its Quick Start guidance.
- `410e8db` — completed the sidebar conversation-menu focus contract with keyboard navigation, trigger restoration, and viewport-safe upward placement.
- `294dcb0` — made the Coding Workspace task brief a persisted, collapsible panel so the file/editor surface remains the primary active-project view.
- `db988de` — clarified the local Marketplace contract with capability-specific availability/status copy and replaced its remaining decorative Unicode icons with the shared SVG icon system.
- `90ba352` — added a host-backed Chat message editing flow with local-only validation, inline draft controls, Save & regenerate behavior, failure-safe restoration, and a dedicated `ChatMessage` component; live Electron acceptance confirmed the edited prompt and regenerated response.

## Current evidence

- Renderer typecheck: passed.
- Renderer production build: passed.
- Node syntax and repository hygiene checks: passed.
- Agent Host build: passed with 0 warnings and 0 errors after a clean Electron restart.
- Full SkaiDesktop.Core test suite: passed.
- Agent Host replacement smoke: passed.
- Chat queue smoke: passed.
- Headless interaction checks: passed for Settings category search/navigation, model discovery/filtering, Image Studio history, Chat markdown/actions/queue, command palette, and responsive overflow at 1920, 1440, 1280, and 960 widths.
- Coding workflow interaction checks: passed for project browser → Terminal handoff, selected-project propagation, Agent Workbench handoff, and responsive overflow at 1920, 1440, 1280, and 960 widths.
- Coding diff interaction checks: passed for local removal/addition preview, guarded save, post-save clean state, and responsive overflow.
- Coding editor tab interaction check: passed for opening two files, switching tabs, preserving the active buffer, blocking close while dirty, and responsive overflow at 960px. Screenshot: `output/playwright/skai-coding-tabs.png`.
- Docked terminal interaction check: passed for opening the terminal inside the selected coding project, forwarding the bounded project command, rendering exit/output feedback, preserving editor tabs, and responsive overflow at 960px. Screenshot: `output/playwright/skai-coding-docked-terminal.png`.
- Project explorer search check: passed for `Ctrl+P` focus, filename/path filtering, no-match recovery, and responsive overflow at 960px. Screenshot: `output/playwright/skai-coding-file-search.png`.
- Memory edit interaction check: passed for loading an existing entry, entering edit mode, updating by stable ID, refreshing persisted content, canceling edit, and responsive overflow at 960px. Screenshot: `output/playwright/skai-memory-edit.png`.
- Memory/Workbench interaction checks: passed for loading, empty state, Memory failure/retry, lifecycle columns, dependency context, and responsive overflow.
- Workbench detail interaction check: passed for opening every task-detail disclosure, keyboard focus on native summaries, dependency/result rendering, approval/failure capability notes, and responsive overflow at 960px. Screenshot: `output/playwright/skai-workbench-details.png`.
- Command palette interaction check: passed for Ctrl+K open, initial focus, ArrowDown selection, filtered Enter navigation, Escape close, focus restoration to the invoking control, and responsive overflow at 1280px. Screenshots: `output/playwright/skai-command-palette-open.png` and `output/playwright/skai-command-palette.png`.
- Theme/backdrop visual audit: passed for light/dark × Deep Sea/Deep Space combinations, readable headings and surfaces, and no horizontal overflow at 1280px. Screenshots: `output/playwright/skai-theme-light-deep-sea.png`, `output/playwright/skai-theme-light-deep-space.png`, `output/playwright/skai-theme-dark-deep-sea.png`, and `output/playwright/skai-theme-dark-deep-space.png`.
- Coding dialog accessibility check: passed for initial field focus, Shift+Tab/Tab wrapping, Escape close, submit close, focus restoration after both close paths, and responsive overflow at 1280px. Screenshot: `output/playwright/skai-coding-dialog-open.png`.
- Unified overlay interaction check: passed for command-palette initial focus, boundary wrapping, Escape restoration, filtered keyboard navigation, Coding dialog initial focus, boundary wrapping, Escape restoration, and responsive overflow at 1280px. Screenshots: `output/playwright/skai-command-palette-unified.png` and `output/playwright/skai-coding-dialog-unified.png`.
- Marketplace interaction check: previously passed for the catalog shell; `947fb93` restores it as a local-first active route with installation/activation still capability-gated.
- Route composition sweep: passed for all nine active routes, active navigation state, route-specific document titles, browser back navigation, and no horizontal overflow at 1280px and 960px using the built renderer.
- Workspace-coordinator regression sweep: passed after extraction for all nine active routes, active navigation state, route-specific document titles, browser back navigation, and no horizontal overflow at 1280px and 960px using the rebuilt renderer.
- Chat runtime regression: passed the existing FIFO queue smoke plus renderer interaction coverage for conversation loading, queued follow-up while the model is busy, indexed removal, completion response rendering, and 1280px overflow.
- Coding runtime regression: passed project-browser, project-terminal, and project-editor smoke tests plus renderer coverage for project selection, file read, diff review, guarded save, docked terminal execution, output rendering, and 1280px overflow. Screenshot: `output/playwright/skai-coding-coordinator.png`.
- Task composer interaction check: passed project selection, task brief entry, disabled Worktree state, local proposed-task creation, Agent Workbench handoff, and no horizontal overflow at 1280px. Screenshot: `output/playwright/skai-coding-task-composer.png`.
- Task review interaction check: passed opening a proposed task's detail disclosure, rendering its persisted brief, and no horizontal overflow at 1280px.
- Settings save interaction check: passed a live field change, backend-confirmed save feedback, staying on Settings, and no horizontal overflow at 1280px. Screenshot: `output/playwright/skai-settings-save-feedback.png`.
- Chat task composer check: passed local `tasks.plan` submission, proposed-task handoff to Workbench, explicit unavailable Worktree state, no Cloud UI, and no horizontal overflow at 1280px. Screenshots: `output/playwright/skai-chat-task-composer-open.png` and `output/playwright/skai-chat-task-composer.png`.
- Conversation selection check: passed selecting a saved chat by ID, restoring its messages without leaving Chat, hiding the prior conversation, and no horizontal overflow at 1280px. Screenshot: `output/playwright/skai-conversation-selection.png`.
- Chat visual refinement check: passed compact contextual hierarchy, removal of the oversized hero, separate conversation/composer geometry, local task composer visibility, no Cloud UI, and no horizontal overflow at 1280px. Screenshot: `output/playwright/skai-chat-compact-header.png`.
- Workbench list/detail check: passed list-first rendering, selected task detail with brief/status/result, Board view toggle, selection restoration, and no horizontal overflow at 1280px. Screenshot: `output/playwright/skai-workbench-list-detail.png`.
- Shared route geometry sweep: passed all nine routes at 1280px and 960px with no horizontal overflow and no Cloud UI. Models visual check: `output/playwright/skai-models-compact-header.png`.
- Memory action-safety check: passed edit/save feedback, canceling removal, confirming removal, persistence refresh, no Cloud UI, and no horizontal overflow at 1280px. Screenshot: `output/playwright/skai-memory-confirmation.png`.
- Terminal empty-state check: passed no-project capability messaging, hidden Run action until a project exists, Coding Workspace handoff, no Cloud UI, no duplicate panel title, and no horizontal overflow at 1280px. Screenshot: `output/playwright/skai-terminal-empty.png`.
- Electron launch/bridge check: passed visible launch from the built Electron binary, secure context-isolated renderer loading, and the `tasks.plan` bridge allowlist update.
- Packaged Electron artifact check: passed portable Windows artifact staging with a self-contained Agent Host, then verified the main Skai navigation/Settings surface and the separate Percy companion/sprite surface through DevTools.
- Image Studio live Electron check: passed fresh renderer launch, local readiness state rendering, prompt recipe application, workflow-required gating, and no Cloud UI. The generate action remains disabled until all real local inputs are present.
- Models focused UI check: passed model discovery, provider/capability metadata rendering, discovered status, exact navigation selection, screenshot capture, and no Cloud UI. Screenshot: `output/playwright/skai-models-provider-status.png`.
- Branded shell check: passed tracked wolf asset loading with preserved transparency, accessible decorative-image treatment, responsive shell rendering, and no Cloud UI. Screenshot: `output/playwright/skai-branded-shell.png`.
- Chat first-run check: passed the branded empty state, hashed asset loading from `dist/assets`, visible composer, fresh Electron renderer launch, and no Cloud UI. Screenshot: `output/playwright/skai-chat-branded-empty.png`.
- Sidebar navigation check: passed all nine route transitions, active-state styling, nine SVG icon instances, no horizontal overflow at 1280px, and no Cloud UI. Screenshot: `output/playwright/skai-sidebar-svg-icons.png`. Fresh Electron process launch also passed; the Windows accessibility surface did not settle to the renderer content on the final restart, so this checkpoint does not claim a live Electron visual screenshot.
- Sidebar resize interaction check: passed default 276px sizing, keyboard resize to the 360px bound, reload persistence, separator ARIA range state, route title preservation, and a 1440px screenshot review. The resize handle is hidden while collapsed and the responsive shell keeps the bounded width above the compact breakpoint.
- Coding Workspace focus check: passed project selection, active-project context rendering, project switching by stable ID, task/files/editor surface continuity, no dashboard overview after selection, and no horizontal overflow at 1440px and 960px. Screenshot reviewed from the running Vite renderer at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-coding-focus.png`.
- Agent Workbench activity check: passed real task-derived summary counts, attention/active/completed/all filtering, selected-task continuity, scheduled-record summary, list rendering, and no horizontal overflow at 1280px and 960px. Screenshot reviewed from the running Vite renderer at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-workbench-filters.png`.
- Settings semantic-search check: passed `endpoint`, `reduced motion`, and `memory` queries, automatic selection of the first matching panel, clearing search to restore all 11 sections, and no horizontal overflow at 1280px and 960px. Screenshot reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-settings-search.png`.
- Chat density/context check: passed active-thread summary rendering, Quick Start visibility only for empty Chat, a 350px active conversation panel bound, and no horizontal overflow at 1440px and 960px. Screenshot reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-chat-current.png`.
- Sidebar conversation-menu check: passed first-action focus, Arrow/Home/End navigation, Escape restoration to the originating trigger, outside-click close, viewport-safe upward placement, and no horizontal overflow at 1280px and 960px. Screenshot reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-sidebar-menu-focus.png`.
- Coding task-panel check: passed default-collapsed task brief behavior, expand/collapse controls, persisted selected-project and panel state after reload, editor preservation, and no horizontal overflow at 1440px and 960px. Screenshot reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-coding-task-panel.png`.
- Workbench recovery guidance check: passed failed-task selection, explicit re-plan guidance, host capability boundary copy, and no horizontal overflow at 1280px. Screenshot reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-workbench-recovery.png`.
- Conversation context-menu check: passed right-click and keyboard-visible menu actions for stable-ID pinning and renaming, persisted title rendering, compact action-row geometry at 1440px, and no Cloud UI. Screenshot: `output/playwright/skai-sidebar-context-menu.png`.
- Chat retry check: passed replacement of the selected assistant response through `chat.complete`, stable conversation persistence, custom-title preservation, failure recovery with the previous response restored, and no Cloud UI. Screenshot: `output/playwright/skai-chat-retry.png`.
- Coding terminal persistence check: passed renderer typecheck/build plus project-browser and guarded-editor smoke coverage for the local coding workflow; the per-project dock preference is stored locally and no Cloud references are present in shipped source. Live Electron visual interaction remains limited by the Windows screenshot/input bridge returning another app's pixels.
- Workbench navigation check: passed renderer typecheck/build, verified local-only source boundaries, and reviewed the focused diff for persisted view/task state and keyboard navigation. Further feature work is paused pending the architecture audit's host lifecycle, provider, and AirLLM gates.
- Electron supervision check: passed `audit:electron-runtime`, repository syntax checks, and diff validation for request timeouts, pending-call cleanup, and process-tree shutdown. C# request cancellation and AirLLM end-to-end verification remain open in the parallel architecture slice.
- Workbench visual checkpoint: `output/playwright/skai-workbench-responsive.png`.
- Visual checkpoints: `output/playwright/skai-image-studio-after.png` and `output/playwright/skai-chat-after.png`.
- Live Electron window: verified open as `Skai Desktop · chat`. The Windows screenshot provider returned Brave pixels while the Electron accessibility surface reported the correct window; this capture mismatch remains unverified and must not be treated as visual acceptance.
- Appearance persistence check: passed seeded light/amber/compact/deep-sea startup state, saved dark/rose/spacious/deep-space settings, reload restoration, reduced-motion persistence, and no horizontal overflow at 1280px. Screenshot reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-appearance-persistence.png`.
- Chat actions menu check: passed compact 1280px toolbar geometry, first/last menu-item keyboard navigation, Escape focus restoration, and no horizontal overflow. Screenshot reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-chat-actions-menu.png`.
- Chat scroll restoration check: passed switching between two long conversations, saving the prior thread’s scroll position, restoring it on return, and no horizontal overflow at 1280px. Screenshot reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-chat-scroll-restoration.png`.
- Memory scope check: passed scope filtering by stable scope value, enabled/disabled status and source rendering, empty-scope recovery, and no horizontal overflow at 1280px. Screenshot reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-memory-scopes.png`.
- Shell theme-token audit: passed dark and light sidebar/toolbar contrast, a real Settings change to light + amber + compact + Deep Sea + reduced motion with local persistence, all eight primary/utility route transitions, system theme resolution under light and dark OS preferences, Deep Space rendering, and no horizontal overflow at 1280px or 960px. Screenshots reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-shell-dark.png`, `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-shell-light-deep-sea.png`, and `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-shell-dark-deep-space.png`.
- Companion-window acceptance: passed a fresh built Electron launch with two independent windows, an always-on-top transparent Percy companion surface, the six-frame Percy walk atlas, explicit pointer-to-screen dragging with persisted bounds, completion bubble delivery, and hide/show control. The live Electron surface was inspected directly; the companion remains local-only and closes with the main window.
- Sidebar project-context check: passed rendering a local project beside workspace navigation, selecting the project into Coding Workspace through its stable ID, preserving the honest local-only copy, and no horizontal overflow at 1440px or 960px. Screenshots reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-after-sidebar-projects-1440.png` and `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-after-sidebar-projects-960.png`; a fresh Electron launch also confirmed the section and empty state with equal body/scroll widths.
- Chat task hierarchy check: passed a collapsed-by-default local task planner, keyboard/button disclosure, persisted open state, preserved task-planning controls when expanded, no renderer errors, and no horizontal overflow at 1440px and 960px. Screenshots reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-chat-hierarchy-after-1440.png`, `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-chat-hierarchy-open-1440.png`, `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-chat-hierarchy-after-960.png`, and the live Electron capture `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-electron-chat-hierarchy.png`; Electron confirmed two windows and the expected 112px collapsed / 563px expanded geometry.
- Coding workspace finish check: replaced coding-surface Unicode glyphs with reusable SVG icons, reduced the active-project header hierarchy, and stacked the file title/actions at compact desktop widths so open-file controls remain readable. Renderer interaction passed project selection, file opening, zero-error rendering, and no horizontal overflow at 1440px and 960px. Screenshots reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-coding-review-after-1440.png`, `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-coding-review-after-960.png`, and the live Electron capture `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-electron-coding-foundation.png`.
- Workbench recovery check: added explicit local handoffs from failed-task detail to Chat re-planning or Coding Workspace while keeping failed-task retry visibly unsupported by the current host. Renderer checks passed list/detail selection, failed recovery actions, attention filtering, optional board view, no renderer errors, and no horizontal overflow at 1440px and 960px. Screenshots reviewed at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-workbench-recovery-failed-1440.png`, `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-workbench-recovery-board-1440.png`, and the live Electron capture `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-electron-workbench.png`.
- Overflow menu focus check: shared Chat `More` menus now focus the first enabled action, safely retain trigger focus when every action is unavailable, wrap ArrowUp/ArrowDown navigation, expose `aria-controls`, and close cleanly on outside click. Renderer interaction passed active and empty conversation states with no errors or horizontal overflow at 1440px and 960px; live Electron smoke passed with two windows and screenshot review at `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-overflow-after-1440.png` and `C:\\Users\\basic\\AppData\\Local\\Temp\\skai-electron-overflow.png`.
- Bridge fallback check: a renderer without the Electron preload now renders an explicit unavailable-host state instead of throwing during pet/agent initialization. The fallback harness passed with no page errors and no horizontal overflow at 1280px.

## Current task

Continue the premium vertical slices, prioritizing the coding workflow and shared shell before decorative polish. Route composition is isolated behind a typed `AppRouteView`; `App.tsx` composes the shell through `useDesktopWorkspace`; chat runtime behavior is behind `useChatWorkspace`; and coding/project runtime behavior is behind `useCodingWorkspace`. The command palette and Coding Workspace dialog share one verified focus/escape/restoration contract, alongside the theme/backdrop contrast pass and the first honest Marketplace discovery shell. Preserve the Electron + React + TypeScript + Vite + .NET Agent Host architecture and all unrelated native/AirLLM changes.

## Outstanding priorities

1. Keep the truthful per-task environment selector and task-oriented composer local-only; Local is functional today, while Worktree must remain visibly unavailable until supported. Hosted model routes and cloud execution are out of scope.
2. Replace remaining utility placeholders with real backend-backed surfaces or explicit unavailable states.
3. Continue improving Memory and Agent Workbench interaction depth with richer recovery actions. Memory editing and Workbench task details are now backed by existing host capabilities; approval controls and failed-task retry remain intentionally unavailable where the host does not expose them.
4. Extend the keyboard-focus pass from the shared modal surfaces and new workspace shortcuts to remaining overlay and menu surfaces, including more real Electron interaction capture.
5. Preserve the route-wide theme coverage now verified in the shared shell, then add packaged Electron visual acceptance for the same combinations.
6. Connect Marketplace installation and activation only when real local runtime capabilities exist; keep the current local discovery shell honest and do not fabricate integrations.

## Scope guard

The following working-tree paths are intentionally not part of the premium UI checkpoints and must remain untouched unless explicitly requested:

- `native/SkaiDesktop.Core.Tests/Program.cs`
- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/ServerRock.Native/**`
- `native/SkaiDesktop.Core.Tests/AirLlmProviderTests.cs`
- `native/SkaiDesktop.Core/Services/AirLlmProvider.cs`
- `services/airllm/**`
- `ServerRock_AI_FINAL_COMPLETE_HANDOFF/**`
- `output/**`
