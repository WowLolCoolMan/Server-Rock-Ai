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

## Current evidence

- Renderer typecheck: passed.
- Renderer production build: passed.
- Node syntax and repository hygiene checks: passed.
- Agent Host build: passed with 0 warnings and 0 errors after a clean Electron restart.
- Full ServerRock.Core test suite: passed.
- Agent Host replacement smoke: passed.
- Chat queue smoke: passed.
- Headless interaction checks: passed for Settings category search/navigation, model discovery/filtering, Image Studio history, Chat markdown/actions/queue, command palette, and responsive overflow at 1920, 1440, 1280, and 960 widths.
- Coding workflow interaction checks: passed for project browser → Terminal handoff, selected-project propagation, Agent Workbench handoff, and responsive overflow at 1920, 1440, 1280, and 960 widths.
- Coding diff interaction checks: passed for local removal/addition preview, guarded save, post-save clean state, and responsive overflow.
- Coding editor tab interaction check: passed for opening two files, switching tabs, preserving the active buffer, blocking close while dirty, and responsive overflow at 960px. Screenshot: `output/playwright/skai-coding-tabs.png`.
- Memory/Workbench interaction checks: passed for loading, empty state, Memory failure/retry, lifecycle columns, dependency context, and responsive overflow.
- Workbench visual checkpoint: `output/playwright/skai-workbench-responsive.png`.
- Visual checkpoints: `output/playwright/skai-image-studio-after.png` and `output/playwright/skai-chat-after.png`.
- Live Electron window: verified open as `Skai Desktop · chat`. The Windows screenshot provider returned Brave pixels while the Electron accessibility surface reported the correct window; this capture mismatch remains unverified and must not be treated as visual acceptance.

## Current task

Continue the premium vertical slices, prioritizing the coding workflow and shared shell before decorative polish. Preserve the Electron + React + TypeScript + Vite + .NET Agent Host architecture and all unrelated native/AirLLM changes.

## Outstanding priorities

1. Integrate the coding workspace into a dockable project workflow with file tabs, terminal output, guarded diff review, and explicit approvals. File tabs are now present with dirty-close protection; dockable terminal panes, broader review, and explicit approval surfaces remain.
2. Replace remaining utility placeholders with real backend-backed surfaces or explicit unavailable states.
3. Continue improving Memory and Agent Workbench interaction depth with editing/detail views and richer recovery actions; loading, failure, and empty states are now covered.
4. Add stronger keyboard-focus and dialog accessibility tests, including real Electron capture when the provider can target the correct window.
5. Audit dark/light/system themes and density/backdrop combinations across every major route.
6. Add workflow/skills/integration surfaces only where the existing runtime has a real capability; do not fabricate integrations.

## Scope guard

The following working-tree paths are intentionally not part of the premium UI checkpoints and must remain untouched unless explicitly requested:

- `native/ServerRock.Core.Tests/Program.cs`
- `native/ServerRock.Core/Models/AppSettings.cs`
- `native/ServerRock.Native/**`
- `native/ServerRock.Core.Tests/AirLlmProviderTests.cs`
- `native/ServerRock.Core/Services/AirLlmProvider.cs`
- `services/airllm/**`
- `ServerRock_AI_FINAL_COMPLETE_HANDOFF/**`
- `output/**`
