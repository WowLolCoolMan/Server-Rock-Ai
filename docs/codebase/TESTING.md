# Testing

## Current verification layers

- Core behavior and persistence tests run through `npm test -- --no-restore` and cover planners, path bounds, models, tasks, approvals, providers, local-only policy, attachments, schedules, Git, and recovery behavior.
- Agent Host compilation is verified with `npm run agent-host:build -- --no-restore`.
- Renderer correctness is currently typechecked and bundled with `npm run desktop:typecheck` and `npm run desktop:build`.
- Repository-level audits cover syntax, hygiene, native UI contracts, Electron runtime policy, local-only policy, and release workflow policy.
- GitHub Actions provide CI/release execution, but the repository does not have a browser-runner or coverage threshold as a standard.

## Test organization

- `native/SkaiDesktop.Core.Tests/` contains the primary automated behavior suite.
- `scripts/` contains Node smoke/audit scripts.
- `native/ServerRock.Native.Tests/` and native acceptance helpers cover selected legacy/native paths.
- UI behavior is mostly validated by build/audit contracts rather than by driving the running Electron app.

## Pet coverage

`PetWanderPlannerTests.cs` gives strong unit-level coverage for the contributed grid planner: deterministic paths, blocked composer space, resize/clamping, tiny windows, gliding, facing, and no-jump behavior. This coverage currently stops at Core. There is no equivalent test that creates the Electron pet BrowserWindow, applies the real screen/composer geometry, resizes or hides it, and asserts the live window remains inside valid cells.

## Current validation result

The review run passed the sequential Core suite, Agent Host build, renderer typecheck/build, syntax, hygiene, native UI, Electron runtime, local-only, and release-workflow checks. A first parallel attempt encountered a transient `SkaiDesktop.Core.dll` file lock while build and test processes overlapped; rerunning sequentially passed. The check suite should document or serialize this dependency to keep CI diagnostics deterministic. [TODO]

## Largest testing gaps

- Electron renderer and main-process end-to-end coverage for routing, command palette, marketplace, chat, coding, and pet interactions.
- Packaged Windows acceptance on a clean machine, including first run, profile migration, updates, signed installer behavior, and recovery.
- DPI, multi-monitor, narrow-window, resize, always-on-top, and accessibility/focus matrix.
- Real provider/download smoke tests with controlled fixtures for Hugging Face, GitHub, ComfyUI, Ollama, and MCP.
- Agent golden-loop tests proving plan → approval → execution → diff → test → commit with durable restart recovery.
- Linux verification; current documentation identifies it as unverified.

## Evidence

- `native/SkaiDesktop.Core.Tests/PetWanderPlannerTests.cs`
- `native/SkaiDesktop.Core.Tests/`
- `scripts/`
- `package.json`
- `.github/workflows/`
- `docs/CAPABILITY_MATRIX.md`
- `docs/PRODUCTION_READINESS_GAPS.md`
