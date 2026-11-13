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

## Current evidence

- Renderer typecheck: passed.
- Renderer production build: passed.
- Node syntax and repository hygiene checks: passed.
- Agent Host build: passed with 0 warnings and 0 errors after a clean Electron restart.
- Full ServerRock.Core test suite: passed.
- Agent Host replacement smoke: passed.
- Chat queue smoke: passed.
- Headless interaction checks: passed for Settings category search/navigation, model discovery/filtering, Image Studio history, Chat markdown/actions/queue, command palette, and responsive overflow at 1920, 1440, 1280, and 960 widths.
- Visual checkpoints: `output/playwright/skai-image-studio-after.png` and `output/playwright/skai-chat-after.png`.
- Live Electron window: verified open as `Skai Desktop · chat`. The Windows screenshot provider returned Brave pixels while the Electron accessibility surface reported the correct window; this capture mismatch remains unverified and must not be treated as visual acceptance.

## Current task

Continue the premium vertical slices, prioritizing the coding workflow and shared shell before decorative polish. Preserve the Electron + React + TypeScript + Vite + .NET Agent Host architecture and all unrelated native/AirLLM changes.

## Outstanding priorities

1. Integrate the coding workspace into a dockable project workflow with file tabs, terminal, guarded diff review, and explicit approvals.
2. Replace remaining utility placeholders with real backend-backed surfaces or explicit unavailable states.
3. Improve Memory and Agent Workbench interaction depth, including loading, failure, and empty states.
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
