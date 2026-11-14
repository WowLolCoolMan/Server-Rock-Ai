# Skai Desktop first-release readiness

Last audited: 2026-11-13

Status: **not ready to publish as the first major release**.

## Ready now

- Electron renderer typecheck and production build pass.
- The persistent Agent Host builds cleanly and its protocol smoke passes.
- Native Core tests pass, including AirLLM fallback policy and the native pet wander planner.
- The native Windows artifact builds and passes bounded startup smoke.
- Electron Settings now has a usable surface for general routing, appearance, model discovery, local voice, local image workflows, agent controls, privacy, storage, updates, and advanced controls.
- The Electron pet uses the existing Byte asset pack, persists its position, supports pointer drag-and-drop, keyboard arrows, wandering, reduced-motion behavior, and a hide control.
- Unsupported cloud routes are clearly labeled and cannot be saved or silently used as local routes.

## Release blockers

1. The Electron runtime is not packaged as an installable artifact. The current native packaging workflow packages the Uno/native executable, while Electron still starts the Agent Host from a repository `.csproj` path. A release package needs a bundled self-contained Agent Host and a packaged Electron startup smoke test.
2. Cloud routing is not implemented. OpenAI API keys, ChatGPT sign-in, OpenAI-compatible cloud services, and ElevenLabs voice need explicit provider adapters, secure credential storage, consent/data-boundary handling, and end-to-end tests. The current host intentionally accepts only local loopback endpoints.
3. Release signing and clean-machine install/upgrade/uninstall evidence are not available in this checkout. The release workflow is fail-closed until signing material and lifecycle evidence are supplied.
4. Linux runtime and packaged acceptance are still unverified in this checkout.

Main remains untouched. This branch is suitable for continuing the release work, but it is not an honest candidate for a public release or a “ready to merge” release PR until the blockers above are closed.
