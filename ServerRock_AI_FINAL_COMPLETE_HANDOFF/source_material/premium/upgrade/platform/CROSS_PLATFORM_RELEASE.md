# Windows and Linux platform parity, distribution, update and recovery

## Existing product
C#/.NET10 + Uno Skia desktop target shared between Windows/Linux, plus Windows App SDK target. Do not install Electron runtime. Windows/Linux distribution must be tested with exact published binaries and clean user profiles. CI compilation does not equal UX/release acceptance.

## Platform capability contracts
| Service | Windows | Linux | Required evidence |
|---|---|---|---|
| Secure secrets | DPAPI/Credential Manager via supported library | Secret Service/keyring where available | Persistent save/revoke, no plaintext fallback |
| Terminal | ConPTY via reviewed library | Unix PTY via reviewed library | Resize, unicode, cancel tree, env redact |
| File/folder picker | Native Win/Uno picker with owner | Portal/GTK/Uno picker | Foreground focus, path scope, cancel no write |
| Notifications | OS integration | Desktop notification/DBus availability | Opt-in, click action and fallback |
| Tray/window | Windows taskbar/tray | X11/Wayland appropriate support | Minimize/restore, hidden state, high DPI |
| Model runtime | loopback API hosts/GPU adapters | loopback API hosts/GPU adapters | No false local label, OOM recovery |
| Packaging | signed per-user installer + verified portable build | signed distro package or user-target AppImage as supported | Install/upgrade/uninstall/rollback from clean machine |
| Update | pinned signed manifest, hash, explicit install | signed manifest + verified download + appropriate app action | No unattended unverified replacement |
| Accessibility | UI Automation/Narrator | AT-SPI/SR where supported | keyboard, focus, reduced motion, contrast |

## OS matrix
- Windows 11 x64: primary acceptance target. Windows 10 only when supported edition/build and actual runtime evidence; Windows 8/8.1 is not a supported .NET 10 claim.
- Linux Ubuntu desktop x64 first; Debian/Fedora/Arch native testing before claiming support. WSLg/Xvfb startup alone is not distro acceptance. Wayland vs X11 differences must be documented.
- Other architectures/OSes are future, not automatic based on targets. Honor no estimated delivery dates.

## Build/check entrypoints from live repository (verify current first)
```
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
npm run audit:native-ui
```
Node commands are development helpers only. A prod release pipeline should create versioned bundles/installers, produce trusted signature/manifest/checksum, smoke the actual artifact and run human GUI acceptance.

## Release checklist
1. Snapshot CI commit/release config; verify dependencies and supply chain, no secrets in artifacts.
2. Run Core + integration tests, UI automation and manual acceptance on real Windows/Linux.
3. Build exact publish artifacts; execute them from clean profiles and check logs/primary window.
4. Verify signed manifest and code signature. Fail closed if missing; checksums without authenticated origin are insufficient.
5. Clean install; model connect; chat; project edit approval; terminal; marketplace install; Git status; update.
6. Upgrade and restart; all conversations/settings/skills/workspaces remain; if migration fails, rollback with intact prior profile.
7. Uninstall without deleting user data by default. Verify explicit clear-data path separately.
8. Offline, locked keyring, unavailable GPU, missing OS dependency, corrupt update and network-loss recovery tested.
9. Record OS/distro/window manager/.NET, exact artifact SHA, result, screenshot and reviewer.

## Accessibility/package smoke
Test at widths 1440×900, 1024×768, 760×560 and 150/200% scaling. Display app and dialogs on correct monitor; menu focus, Tab/Esc, overlay stacking and native picker focus. Test PDF/import preview and cancel, archive idempotency, and keyboard restoration.
