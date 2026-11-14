# Stack

## Product/runtime

- Electron main process with a React + TypeScript renderer is the current desktop product surface.
- A persistent .NET 10 Agent Host is started by Electron and communicates over JSON-lines stdio.
- `native/SkaiDesktop.Core` is the shared service/domain layer for local models, projects, terminal, Git, memory, skills, MCP, images, voice, approvals, and persistence.
- `native/ServerRock.Native` is a legacy Uno/XAML compatibility surface. The current product direction in `README.md` is Electron + React; Uno is described as migration compatibility rather than the primary shell.

## Toolchain

- Node/npm, Vite, TypeScript, React, Electron, `concurrently`, and `wait-on` are used by `apps/desktop`.
- .NET SDK 10 is used by `native/SkaiDesktop.Core`, `native/SkaiDesktop.Core.Tests`, `services/agent-host`, and the legacy Uno project.
- C# dependencies include PdfPig, Porta.Pty, ProtectedData, XTerm.NET, Markdig, and SVG/Uno packages.
- GitHub Actions provide CI and release checks.

## Commands

- `npm run desktop:install`
- `npm run desktop:dev`
- `npm run desktop:build`
- `npm run desktop:typecheck`
- `npm run agent-host:build`
- `npm test -- --no-restore`
- `npm run check:syntax`
- `npm run check:hygiene`
- `npm run audit:native-ui`
- `npm run audit:electron-runtime`
- `npm run audit:local-only`
- `npm run audit:release-workflow`

## Configuration and state

- Electron can use `SERVER_ROCK_RENDERER_URL` for a development renderer URL.
- Profile and storage paths are resolved through `SRAI_*_PATH` variables in tests and supporting scripts.
- Local services are expected on loopback endpoints such as Ollama, LM Studio/OpenAI-compatible servers, and ComfyUI.
- There is no repository `.env.example` or `.env.template`; deployment-specific configuration is therefore not documented in one discoverable contract. [TODO]

## Evidence

- `README.md`
- `apps/desktop/package.json`
- `apps/desktop/electron/main.cjs`
- `apps/desktop/src/main.tsx`
- `services/agent-host/ServerRock.AgentHost.csproj`
- `native/SkaiDesktop.Core/SkaiDesktop.Core.csproj`
- `native/ServerRock.Native/ServerRock.Native.csproj`
- `.github/workflows/`
