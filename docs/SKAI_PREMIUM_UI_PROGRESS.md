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
- Docked terminal interaction check: passed for opening the terminal inside the selected coding project, forwarding the bounded project command, rendering exit/output feedback, preserving editor tabs, and responsive overflow at 960px. Screenshot: `output/playwright/skai-coding-docked-terminal.png`.
- Project explorer search check: passed for `Ctrl+P` focus, filename/path filtering, no-match recovery, and responsive overflow at 960px. Screenshot: `output/playwright/skai-coding-file-search.png`.
- Memory edit interaction check: passed for loading an existing entry, entering edit mode, updating by stable ID, refreshing persisted content, canceling edit, and responsive overflow at 960px. Screenshot: `output/playwright/skai-memory-edit.png`.
- Memory/Workbench interaction checks: passed for loading, empty state, Memory failure/retry, lifecycle columns, dependency context, and responsive overflow.
- Workbench detail interaction check: passed for opening every task-detail disclosure, keyboard focus on native summaries, dependency/result rendering, approval/failure capability notes, and responsive overflow at 960px. Screenshot: `output/playwright/skai-workbench-details.png`.
- Command palette interaction check: passed for Ctrl+K open, initial focus, ArrowDown selection, filtered Enter navigation, Escape close, focus restoration to the invoking control, and responsive overflow at 1280px. Screenshots: `output/playwright/skai-command-palette-open.png` and `output/playwright/skai-command-palette.png`.
- Theme/backdrop visual audit: passed for light/dark × Deep Sea/Deep Space combinations, readable headings and surfaces, and no horizontal overflow at 1280px. Screenshots: `output/playwright/skai-theme-light-deep-sea.png`, `output/playwright/skai-theme-light-deep-space.png`, `output/playwright/skai-theme-dark-deep-sea.png`, and `output/playwright/skai-theme-dark-deep-space.png`.
- Coding dialog accessibility check: passed for initial field focus, Shift+Tab/Tab wrapping, Escape close, submit close, focus restoration after both close paths, and responsive overflow at 1280px. Screenshot: `output/playwright/skai-coding-dialog-open.png`.
- Unified overlay interaction check: passed for command-palette initial focus, boundary wrapping, Escape restoration, filtered keyboard navigation, Coding dialog initial focus, boundary wrapping, Escape restoration, and responsive overflow at 1280px. Screenshots: `output/playwright/skai-command-palette-unified.png` and `output/playwright/skai-coding-dialog-unified.png`.
- Workbench visual checkpoint: `output/playwright/skai-workbench-responsive.png`.
- Visual checkpoints: `output/playwright/skai-image-studio-after.png` and `output/playwright/skai-chat-after.png`.
- Live Electron window: verified open as `Skai Desktop · chat`. The Windows screenshot provider returned Brave pixels while the Electron accessibility surface reported the correct window; this capture mismatch remains unverified and must not be treated as visual acceptance.

## Current task

Continue the premium vertical slices, prioritizing the coding workflow and shared shell before decorative polish. The command palette and Coding Workspace dialog now share one verified focus/escape/restoration contract, alongside the theme/backdrop contrast pass. Preserve the Electron + React + TypeScript + Vite + .NET Agent Host architecture and all unrelated native/AirLLM changes.

## Outstanding priorities

1. Integrate the coding workspace into a dockable project workflow with file tabs, terminal output, guarded diff review, and explicit approvals. File tabs and a docked terminal panel now share the selected project; broader review and explicit approval surfaces remain.
2. Replace remaining utility placeholders with real backend-backed surfaces or explicit unavailable states.
3. Continue improving Memory and Agent Workbench interaction depth with richer recovery actions. Memory editing and Workbench task details are now backed by existing host capabilities; approval controls and failed-task retry remain intentionally unavailable where the host does not expose them.
4. Extend the keyboard-focus pass from the shared modal surfaces to remaining overlay and menu surfaces, including real Electron capture when the provider can target the correct window.
5. Extend the theme audit from Settings to every major route, including system appearance, density, reduced motion, and backdrop combinations.
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
