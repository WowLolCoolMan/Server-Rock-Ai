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
- ChatGPT sign-in and ElevenLabs remain honestly labeled as unavailable rather than presenting a fake connection.

## Hosted evidence

- [Electron replacement CI run 43550157254](https://github.com/WowLolCoolMan/skai-desktop/actions/runs/43550157254) passed the packaged Linux Electron smoke under Xvfb.
- [Native app CI run 43550157272](https://github.com/WowLolCoolMan/skai-desktop/actions/runs/43550157272) passed on both Ubuntu and Windows.

## Release blockers

1. A signed Electron installer/release asset and clean-machine install/upgrade/uninstall evidence are not available in this checkout. The release workflow now packages and smoke-tests Electron for Windows and Linux, but remains fail-closed until signing material and lifecycle evidence are supplied.
2. ChatGPT sign-in is not implemented. It requires a registered OAuth client and eligible ChatGPT plan-use scopes; the current build deliberately does not simulate that flow.
3. ElevenLabs voice is not implemented. The first release has working native voice, while cloud voice still needs its provider adapter, secure key UX, audio playback, and end-to-end tests.
Main remains untouched. This branch is suitable for continuing the release work, but it is not an honest candidate for a public release or a “ready to merge” release PR until the blockers above are closed.
