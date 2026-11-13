# Technology Stack

## Core Sections (Required)

### 1) Runtime Summary

| Area | Value | Evidence |
|------|-------|----------|
| Primary language | C# for the native product and Core; TypeScript/React for the migration surface; JavaScript for repository scripts | `native/`, `apps/desktop/`, `package.json` |
| Runtime + version | .NET 10; Node.js scripts; Uno Platform targets `net10.0-desktop` and `net10.0-windows10.0.26100` | `native/ServerRock.Native/ServerRock.Native.csproj`, `native/SkaiDesktop.Core/SkaiDesktop.Core.csproj` |
| Package manager | NuGet/MSBuild for .NET; npm for root and Electron migration packages | `native/Directory.Packages.props`, `package.json`, `apps/desktop/package.json` |
| Module/build system | MSBuild/Uno single-project native app plus npm scripts and GitHub Actions | `native/ServerRock.Native/ServerRock.Native.csproj`, `package.json`, `.github/workflows/` |

### 2) Production Frameworks and Dependencies

| Dependency | Version | Role in system | Evidence |
|------------|---------|----------------|----------|
| Uno Platform | centrally managed | Cross-platform XAML/native UI | `native/Directory.Packages.props`, `native/ServerRock.Native/ServerRock.Native.csproj` |
| .NET Windows App SDK | centrally managed | Windows target and WinUI surface | `native/Directory.Packages.props`, `native/ServerRock.Native/ServerRock.Native.csproj` |
| System.Text.Json | .NET runtime | Bounded local persistence and protocol JSON | `native/SkaiDesktop.Core/Infrastructure/`, `native/SkaiDesktop.Core/Services/` |
| React + TypeScript | migration package manifests | Electron renderer prototype | `apps/desktop/package.json`, `apps/desktop/src/` |

### 3) Development Toolchain

| Tool | Purpose | Evidence |
|------|---------|----------|
| `dotnet` | Core tests and native builds | `package.json`, `native/SkaiDesktop.Core.Tests/` |
| Node `--check` | Script syntax gate | `package.json` (`check:syntax`) |
| GitHub Actions | CI, artifacts, platform acceptance and release gates | `.github/workflows/` |

### 4) Key Commands

```bash
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore
dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore
npm run audit:native-ui
npm run check:release-config
```

### 5) Environment and Config

- Config sources: `native/Directory.Build.props`, `packaging/native/release-manifest.json`, local application-data stores, and provider settings.
- Required env vars: none for ordinary local execution; signing variables are required only when release signing is explicitly enabled by `scripts/validate-release-config.cjs`.
- Deployment/runtime constraints: Linux SDK/runtime availability and exact packaged-artifact acceptance are `[TODO]`; Windows native build is locally available.

### 6) Evidence

- `package.json`
- `native/ServerRock.Native/ServerRock.Native.csproj`
- `native/Directory.Packages.props`
- `.github/workflows/native-ci.yml`
- Codebase scan command output from the required acquisition workflow
