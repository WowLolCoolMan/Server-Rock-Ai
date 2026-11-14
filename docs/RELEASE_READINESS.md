# Skai Desktop first-release readiness

Last audited: 2026-11-13

Status: **not ready to publish as the first major release**.

## Ready now

- Electron renderer typecheck and production build pass.
- The persistent Agent Host builds cleanly and its protocol smoke passes.
- Native Core tests pass, including AirLLM fallback policy and the native pet wander planner.
- The native Windows artifact builds and passes bounded startup smoke.
- The portable Electron artifact now bundles the renderer, Electron runtime, and a self-contained Agent Host; its startup smoke verifies the rendered settings and pet surfaces.
- Electron Settings now has a usable surface for general routing, appearance, model discovery, local voice, local image workflows, agent controls, privacy, storage, updates, and advanced controls.
- The Electron pet uses the existing Byte asset pack, persists its position, supports pointer drag-and-drop, keyboard arrows, wandering, reduced-motion behavior, and a hide control.
- Explicit HTTPS OpenAI-compatible cloud model routes are now supported behind a visible consent toggle; provider API keys stay in the OS credential store and never enter settings.json or browser storage.
- ChatGPT sign-in remains honestly labeled as unavailable rather than presenting a fake connection. ElevenLabs cloud voice is now an explicit, opt-in host adapter with OS credential storage, bounded MP3 playback, settings controls, and mocked transport coverage.

## Hosted evidence

- [Electron replacement CI run 43556340130](https://github.com/WowLolCoolMan/skai-desktop/actions/runs/43556340130) passed for the cloud-voice capability commit.
- [Native app CI run 43556340137](https://github.com/WowLolCoolMan/skai-desktop/actions/runs/43556340137) passed on both Ubuntu and Windows for that same capability commit.
- The documentation follow-up is also green: [Electron run 43556661094](https://github.com/WowLolCoolMan/skai-desktop/actions/runs/43556661094) and [Native run 43556661102](https://github.com/WowLolCoolMan/skai-desktop/actions/runs/43556661102) both passed on this branch.

## Release blockers

1. A signed Electron installer/release asset and clean-machine install/upgrade/uninstall evidence are not available in this checkout. The release workflow now packages and smoke-tests Electron for Windows and Linux, but remains fail-closed until signing material and lifecycle evidence are supplied.
2. ChatGPT sign-in is not implemented. It requires a registered OAuth client and eligible ChatGPT plan-use scopes; the current build deliberately does not simulate that flow.
3. A live ElevenLabs account test is still environment-dependent. The code path is implemented and covered with a deterministic transport test; release QA still needs a real key/voice account if cloud voice is advertised in the release checklist.
The current cloud/settings commit is only on `codex/skai-current-worktree`. Remote `main` already contains earlier automation merges (`2a4ec4f` and `77ac2fe`) from before this release audit; restoring that history would require an explicitly approved protected-branch rewrite. This branch is not an honest candidate for a public release or a “ready to merge” release PR until the blockers above are closed.
