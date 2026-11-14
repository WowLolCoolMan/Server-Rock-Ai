# Skai Premium UI Transformation

This ledger records the evidence-backed state of the active frontend migration. It is intentionally honest about partial and unverified work.

## Completed checkpoints

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
- Image Studio live Electron check: passed fresh renderer launch, local readiness state rendering, prompt recipe application, workflow-required gating, and no Cloud UI. The generate action remains disabled until all real local inputs are present.
- Models focused UI check: passed model discovery, provider/capability metadata rendering, discovered status, exact navigation selection, screenshot capture, and no Cloud UI. Screenshot: `output/playwright/skai-models-provider-status.png`.
- Branded shell check: passed tracked wolf asset loading with preserved transparency, accessible decorative-image treatment, responsive shell rendering, and no Cloud UI. Screenshot: `output/playwright/skai-branded-shell.png`.
- Chat first-run check: passed the branded empty state, hashed asset loading from `dist/assets`, visible composer, fresh Electron renderer launch, and no Cloud UI. Screenshot: `output/playwright/skai-chat-branded-empty.png`.
- Sidebar navigation check: passed all nine route transitions, active-state styling, nine SVG icon instances, no horizontal overflow at 1280px, and no Cloud UI. Screenshot: `output/playwright/skai-sidebar-svg-icons.png`. Fresh Electron process launch also passed; the Windows accessibility surface did not settle to the renderer content on the final restart, so this checkpoint does not claim a live Electron visual screenshot.
- Conversation context-menu check: passed right-click and keyboard-visible menu actions for stable-ID pinning and renaming, persisted title rendering, compact action-row geometry at 1440px, and no Cloud UI. Screenshot: `output/playwright/skai-sidebar-context-menu.png`.
- Chat retry check: passed replacement of the selected assistant response through `chat.complete`, stable conversation persistence, custom-title preservation, failure recovery with the previous response restored, and no Cloud UI. Screenshot: `output/playwright/skai-chat-retry.png`.
- Coding terminal persistence check: passed renderer typecheck/build plus project-browser and guarded-editor smoke coverage for the local coding workflow; the per-project dock preference is stored locally and no Cloud references are present in shipped source. Live Electron visual interaction remains limited by the Windows screenshot/input bridge returning another app's pixels.
- Workbench navigation check: passed renderer typecheck/build, verified local-only source boundaries, and reviewed the focused diff for persisted view/task state and keyboard navigation. Further feature work is paused pending the architecture audit's host lifecycle, provider, and AirLLM gates.
- Electron supervision check: passed `audit:electron-runtime`, repository syntax checks, and diff validation for request timeouts, pending-call cleanup, and process-tree shutdown. C# request cancellation and AirLLM end-to-end verification remain open in the parallel architecture slice.
- Workbench visual checkpoint: `output/playwright/skai-workbench-responsive.png`.
- Visual checkpoints: `output/playwright/skai-image-studio-after.png` and `output/playwright/skai-chat-after.png`.
- Live Electron window: verified open as `Skai Desktop · chat`. The Windows screenshot provider returned Brave pixels while the Electron accessibility surface reported the correct window; this capture mismatch remains unverified and must not be treated as visual acceptance.

## Slice — Connect Marketplace local skill library management

Status: `implemented_build_smoke_verified`

The Marketplace Skills surface now has a real local-library journey instead of
stopping at GitHub discovery. Users can choose a local folder, review bounded
metadata and required permissions, inspect the computed SHA-256 digest, install
with pinned local provenance, explicitly enable or disable a skill, and roll an
updated package back to its prior activation. GitHub results remain review-only;
the host does not imply that an unpinned remote repository is safe to install.

Changed files:

- `apps/desktop/src/features/catalog/CatalogPages.tsx`
- `apps/desktop/src/styles.css`
- `apps/desktop/electron/main.cjs`
- `services/agent-host/Protocol/AgentHostLoop.cs`
- `native/SkaiDesktop.Core/Services/SkillPackageInstaller.cs`
- `scripts/smoke-agent-host.cjs`

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `dotnet build services/agent-host/ServerRock.AgentHost.csproj --no-restore -p:BaseOutputPath=bin\\VerifyAgentHost\\ -p:IntermediateOutputPath=obj\\VerifyAgentHost\\` — passed with 0 warnings and 0 errors.
- `SRAI_AGENT_HOST_OUTPUT=services/agent-host/bin/VerifyAgentHost/Debug/net10.0 node scripts/smoke-agent-host.cjs` — passed, including skill preview, digest-pinned install, explicit enablement, update detection, and rollback.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed, including installer safety and rollback coverage.
- `npm run audit:future-features`, `npm run audit:electron-runtime`, `npm run check:hygiene`, and `git diff --check` — passed.

Remaining: authenticated GitHub acquisition, publisher trust/signature policy,
and live packaged Windows install/enable/rollback acceptance remain open. The
current UI labels those paths honestly and keeps remote results review-only.

## Current task

Continue the premium vertical slices, prioritizing the coding workflow and shared shell before decorative polish. Route composition is isolated behind a typed `AppRouteView`; `App.tsx` composes the shell through `useDesktopWorkspace`; chat runtime behavior is behind `useChatWorkspace`; and coding/project runtime behavior is behind `useCodingWorkspace`. The command palette and Coding Workspace dialog share one verified focus/escape/restoration contract, alongside the theme/backdrop contrast pass and the first honest Marketplace discovery shell. Preserve the Electron + React + TypeScript + Vite + .NET Agent Host architecture and all unrelated native/AirLLM changes.

## Outstanding priorities

1. Keep the truthful per-task environment selector and task-oriented composer local-only; Local is functional today, while Worktree must remain visibly unavailable until supported. Hosted model routes and cloud execution are out of scope.
2. Replace remaining utility placeholders with real backend-backed surfaces or explicit unavailable states.
3. Continue improving Memory and Agent Workbench interaction depth with richer recovery actions. Memory editing and Workbench task details are now backed by existing host capabilities; approval controls and failed-task retry remain intentionally unavailable where the host does not expose them.
4. Extend the keyboard-focus pass from the shared modal surfaces and new workspace shortcuts to remaining overlay and menu surfaces, including more real Electron interaction capture.
5. Extend the theme audit from Settings to every major route, including system appearance, density, reduced motion, and backdrop combinations.
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

## Slice — Expand Marketplace model discovery across providers

Status: `implemented_build_smoke_verified`

Marketplace model discovery now accepts an explicit provider filter and merges
Hugging Face lanes with the existing bounded ModelScope GGUF search for LLMs.
Voice and image lanes remain Hugging Face-backed, while the UI labels provider,
task, quantization, size, downloads, and source links per result. Skills and
MCP cards were also corrected to reflect their current host-backed catalog and
local-library boundaries instead of stale “pending” copy.

Evidence:

- Renderer typecheck and production build passed.
- Isolated Agent Host build passed with 0 warnings and 0 errors.
- Disposable Agent Host smoke passed provider-filter validation, including an
  accepted ModelScope lane and rejection of an unknown provider.
- Existing Core test suite and `git diff --check` remain green.

Remaining: model download/import actions are still runtime-specific and must be
surfaced only when the selected local Ollama or LM Studio adapter is available;
remote catalogs currently remain safe discovery/source-link surfaces.

## Slice — Complete local skill management controls

Status: `implemented_build_smoke_verified`

The installed Skills library now supports in-app instruction inspection and an
explicit remove flow in addition to enable/disable, update, and rollback. The
Agent Host reads only the bounded Markdown instruction file, exposes it as
review text, and removes only the owned active package and its owned backups
after the user confirms removal.

Evidence:

- `npm run desktop:typecheck` — passed.
- Isolated Agent Host build — passed with 0 warnings and 0 errors.
- Full Core tests — passed, including owned-package uninstall coverage.
- Disposable Agent Host smoke — passed through preview, install, enable,
  update, rollback, inspect, remove, and final empty-library verification.

Remaining: remote pinned GitHub acquisition, publisher trust/signature policy,
and packaged Windows acceptance remain intentionally open.

## Slice — Connect gated model acquisition actions

Status: `implemented_build_smoke_verified`

Marketplace results now expose real acquisition actions when the selected local
runtime supports the provider: Hugging Face downloads target LM Studio, and
ModelScope GGUF packages import through Ollama. Transfers use a long-running
Electron bridge timeout, host-side local-endpoint validation, provider-specific
identifier validation, and the existing Core checksum/import boundaries. When
the runtime does not match, the UI keeps the safe provider source link instead
of presenting a misleading download button.

Evidence:

- Renderer typecheck passed.
- Isolated Agent Host build passed with 0 warnings and 0 errors.
- Host smoke passed provider capability and unknown-provider rejection for both
  discovery and acquisition routes.
- `git diff --check` passed.

Remaining: a live model transfer requires a running compatible LM Studio or
Ollama runtime; no external model was downloaded during this verification.

## Slice — Add immutable GitHub skill inspection

Status: `implemented_build_smoke_verified`

Curated GitHub skill rows now open a host-backed review surface instead of
jumping directly to a source link. The Agent Host resolves repository metadata,
license, default branch, and the current 40-character commit SHA, then reads
bounded `skill.json` and `SKILL.md` content at that immutable revision. The UI
shows publisher, license, revision, manifest/instruction status, dependencies,
declared tools, required permissions, compatibility, and the reviewed
instructions as untrusted text. Remote GitHub installation remains explicitly
review-only; local validated-folder installation is still the only install path.

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- Isolated Agent Host build — passed with 0 warnings and 0 errors.
- Disposable Agent Host smoke — passed capability advertisement and unsafe
  repository rejection for `marketplace.skills.inspect`.
- `git diff --check` — passed.

Remaining: publisher trust/signature policy and remote pinned package
installation remain intentionally open; no remote package is executed or
installed by this slice.

## Slice — Align shell continuity and model manager

Status: `implemented_build_visual_verified`

The universal shell now exposes all persisted conversations through a scrollable
sidebar with separate pinned and recent sections instead of truncating the list
to six items. Conversation drafts are keyed by conversation identity, so
switching threads cannot overwrite another thread's unsent work.

The Models workspace now follows the reference model-manager composition:
summary tiles, a searchable local inventory, observed runtime connections, and
an explicit provider-selection policy. Counts and readiness are derived from
the host-reported inventory; an empty runtime remains visibly empty rather than
showing sample models as if they were installed.

The chat conversation viewport now has independent scrolling and stores a
thread-specific scroll offset locally for restoration when returning to a
conversation.

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run audit:electron-runtime` — passed.
- `npm run audit:future-features` — passed (161 items, 12 sections, 27 checked).
- `npm run check:hygiene` — passed.
- `git diff --check` — passed.
- Reopened the production Electron build and visually inspected the live
  reference-aligned Models screen through the desktop accessibility tree and
  screenshot surface.

Remaining: full-size visual captures and interaction acceptance across every
reference workspace and supported window size remain open; this slice does not
claim completion of the broader redesign.

## Slice — Recompose Image Studio around prompt-first generation

Status: `implemented_build_visual_verified`

Image Studio now follows the supplied reference composition: recent session
outputs occupy the main workspace while a compact right-side New generation
panel keeps model/workflow, prompt, aspect ratio, output, advanced ComfyUI
configuration, readiness, and generation controls together. The surface only
renders returned session outputs and explicitly explains the local workflow
boundary when no image exists.

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `git diff --check` — passed.

## Slice — Replace duplicate destination sidebar with project/thread shell

Status: `implemented_build_verified`

The desktop shell now separates navigation responsibilities. The far-left rail
is reserved for Home, Image Gen, Usage, Settings, command palette, and profile
access. The adjacent sidebar is project/conversation focused: it contains the
workspace selector, New chat, conversation search, expandable projects,
project-linked conversations, pinned/recent sections, and a live background
task status affordance. Coding, Agent Workbench, Models, Marketplace, Terminal,
Memory, Voice Studio, Permissions, and First-run setup remain reachable from a
workspace-tools menu and the command palette without returning to a second
full-size destination menu.

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `git diff --check` — passed.

Remaining: reopen the dev Electron process for a fresh visual pass at desktop
and narrow widths; the previous live window closed before the rebuilt shell
could be inspected after HMR.

## Slice — Make marketplace discovery and management visible

Status: `implemented_build_verified`

Marketplace now exposes explicit Discover, Skills, Tools & MCP, My library,
and Updates views in the page itself. The discovery view keeps the model source,
size, quantization, abliterated/uncensored, and skill-lane filters visible;
library and update views surface installed-skill management and rollback state
without hiding those capabilities behind a route or command.

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `git diff --check` — passed.

Remaining: add a fresh Electron screenshot of each marketplace view after the
next runtime restart; remote result counts remain dependent on provider health
and network availability.
- Reopened the production Electron build and inspected the live Image Studio
  accessibility tree and foreground screenshot. The tree exposed the prompt,
  recipe buttons, model/workflow selector, aspect ratio selector, output
  selector, advanced toggle, and disabled generation state while configuration
  was incomplete.

Remaining: durable image job/history storage and a live ComfyUI generation
acceptance run still require a configured local image runtime; the UI does not
claim either capability without host evidence.

## Slice — Add honest agent lifecycle metrics

Status: `implemented_build_visual_verified`

The Agent Workbench now follows the reference hierarchy with four summary
tiles—Queued, Awaiting approval, In progress, and Completed—derived directly
from the durable task records. The existing list/board views, selected-task
details, task actions, and scheduled-run controls remain intact. Empty states
describe the actual host-backed state instead of using example counts.

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `git diff --check` — passed.
- Reopened the production Electron build and visually inspected the live Agent
  Workbench accessibility tree and foreground screenshot, including real task
  records and the new four-tile summary.

Remaining: approval controls are still constrained by the current host contract;
the workbench correctly keeps unsupported approval actions informational rather
than implying execution.

## Slice — Make auxiliary reference workspaces reachable

Status: `implemented_build_verified`

The shell now exposes the reference-aligned First-run setup, Permissions, and
Voice Studio surfaces through the sidebar and command palette. Onboarding uses
the existing provider, endpoint, model inventory, and discovery actions;
Permissions reports the current local policy and an honest empty approval queue;
Voice Studio uses the existing native speech service for local profile/runtime
selection, preview, stop, and opt-in output settings. Unsupported speech
recognition and interruption states remain explicitly marked rather than being
presented as implemented.

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `git diff --check` — passed.

Remaining: the desktop computer-use inspection was stopped by the Escape-key
guard before these three newly added routes could be visually captured. A
follow-up live interaction pass should cover the three routes at desktop and
narrow widths; the production build and TypeScript route coverage are green.

## Slice — Add a live coding activity inspector

Status: `implemented_build_verified`

The coding workspace now mirrors the reference editor composition more closely
with a right-side Workspace Activity inspector beside the file explorer/editor.
Its progress, file context, terminal state, and review requirement are derived
from the current project session, so it remains useful when the Agent Host is
idle and never invents task telemetry.

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `git diff --check` — passed.

## Slice — Complete the premium shell and marketplace continuity pass

Status: `implemented_build_tests_visual_verified`

This pass closes the remaining workflow continuity gaps called out by the
reference pack. The desktop now keeps a fixed global rail, a resizable project
and conversation sidebar, and route-specific workspaces behind one consistent
shell. Chat is an open canvas with a persistent composer and contextual
controls; the marketplace is a real multi-provider discovery surface with
Discover, Skills, Tools & MCP, My library, and Updates views. Model filters
surface provider, size, quantization, and explicitly labeled abliterated /
uncensored results, while curated GitHub skills retain review, install, enable,
disable, inspect, and rollback controls.

The companion window is initialized and clamped to the rail/sidebar dock so a
single Percy instance does not obscure the active workspace or composer. The
marketplace detail column was also tightened for compact workspace widths and
the featured surface was moved to a neutral graphite treatment instead of a
purple gradient.

The authored GitHub catalog is now part of the host assembly as a reviewed
250-entry inventory from the existing marketplace source material. Search
combines that local authored inventory with live GitHub lane polling, and each
entry remains inspect-first with immutable revision and manifest checks before
any local package can be enabled. Usage, Settings, Memory, and Terminal also
now use the same graphite surface contract as the newer workspaces.

Evidence:

- `npm test` — passed (native core checks).
- `npm run check:syntax` — passed.
- `npm run check:hygiene` — passed (1108 tracked files).
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `dotnet build services/agent-host/ServerRock.AgentHost.csproj --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:future-features` — passed (161 items, 12 sections, 27 checked).
- `npm run audit:electron-runtime` — passed.
- `npm run audit:local-only` — passed.
- Packaged Electron visual evidence: `C:\Users\basic\AppData\Local\Temp\codex-shot-2026-11-14_10-44-34.png` (Usage) and the live Settings route were inspected after restart.
- `git diff --check` — passed.
- Reopened the production Electron build and inspected live screenshots and
  accessibility trees for Marketplace and Chat. The marketplace exposed live
  Hugging Face results, multi-provider controls, catalog tabs, and an
  `Agent ready` host state; Chat exposed the fixed rail, thread sidebar,
  selected model, contextual actions, persistent composer, and one docked
  companion.

Remaining: provider result counts and image/voice generation availability still
depend on network health and configured local runtimes. Those states remain
explicit in the UI and are not represented as completed capability.

## Slice — Refine chat canvas and workspace launcher continuity

Status: `implemented_build_visual_verified`

The chat route now renders its conversation history on an open canvas instead
of an oversized bordered conversation card. History owns the scrolling region,
the composer remains visible and centered, and empty conversations retain a
quiet centered start state. The workspace selector is now grouped by Build,
Explore, and Manage intent, supports ArrowUp/ArrowDown navigation, closes with
Escape, and returns focus to its trigger.

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.
- Live Electron inspection at the rebuilt 1440×900 desktop window verified the
  open chat canvas, visible composer, grouped launcher, and populated
  marketplace. The supplied `chat.png` remains the before/reference frame;
  the live Electron capture is the after state.

Remaining: the broader migration gates in `docs/ELECTRON_MIGRATION_CUTOVER.md`
remain open for full host-backed feature parity, narrow-window screenshots,
and release packaging validation.

## Slice — Expose chat grounding and response controls end to end

Status: `implemented_host_renderer_build_tests_visual_verified`

The migrated Electron chat now exposes the Core capabilities that were already
implemented but unreachable from the UI: response effort budgets, bounded
full-text attachment context, opt-in web research, and documents-only
grounding. Chat Tools provides the per-request controls, Agents settings
provides durable defaults, and the typed renderer request forwards every choice
through the sandboxed Agent Host boundary. Documents-only mode requires a text
or PDF attachment, suppresses project/memory context, and always disables web
research. Web snippets are bounded and explicitly marked as untrusted context.

Evidence:

- `npm test` — passed (native Core suite, including attachment, prompt, and web-search contracts).
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `dotnet build services/agent-host/ServerRock.AgentHost.csproj --no-restore` — passed with 0 warnings and 0 errors.
- Live Electron inspection verified the Chat Tools panel, documents-only safety note, and Agents settings controls.
- `git diff --check` — passed.

Privacy/security/licensing: web research remains opt-in, uses bounded public
search snippets, and cannot run in documents-only mode. Attachment and project
content stays behind the existing provider and local-route consent boundary.

Remaining: streamed source cards and full task/tool execution parity are still
separate migration slices; this change does not pretend web pages were opened
or tools were executed.

## Slice — Make queued agent tasks executable

Status: `implemented_host_renderer_build_tests`

The Agent Workbench now has a real local execution path for queued tasks. A
user can queue a plan and choose `Run locally`; the Agent Host claims the
specific dependency-ready task, sends its bounded instruction to the selected
local model, and durably records a completed or failed result with provenance
labels. The path is deliberately read-only with respect to project files and
commands: model output can propose work, while existing reviewed write and
terminal contracts remain separate approval boundaries.

The execution contract is exposed through `tasks.execute`, shown in runtime
capabilities, and rendered as an actionable workbench control. Local endpoint
enforcement prevents an agent task from silently switching to a remote route.

Evidence:

- `npm test` — passed.
- `npm run desktop:typecheck` — passed.
- `dotnet build services/agent-host/ServerRock.AgentHost.csproj --no-restore` — passed with 0 warnings and 0 errors.

Remaining: connect task execution to the durable workflow approval graph and
reviewed worktree-backed file changes before the workbench can execute coding
edits or terminal actions.
