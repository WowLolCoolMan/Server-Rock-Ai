# Native release acceptance

This is the release handoff checklist for the Uno/.NET desktop migration. It distinguishes build artifacts from installers and supported releases. A successful publish does not prove that a package installs cleanly or runs on every Linux distribution or Windows version.

## Current artifact path

Run the **Native desktop artifacts** GitHub Actions workflow manually. It publishes self-contained `net10.0-desktop` x64 builds on matching Windows and Ubuntu runners, archives the outputs, generates SHA-256 checksums, and stores the archives as short-lived workflow artifacts. The workflow does not create a GitHub Release, sign binaries, or run installers. Build commands follow the [Uno desktop publishing guide](https://platform.uno/docs/articles/uno-publishing-desktop.html); self-contained RID-specific deployment bundles the .NET runtime, not all native OS libraries.

## Evidence ledger

| Gate | Evidence on this host | Still required before native retirement |
|---|---|---|
| Native build and Core behavior | Windows desktop/Windows App SDK builds and the native Core suite pass; the full Electron/Playwright suite passes 97 tests. | Repeat from the exact release artifacts in CI and attach immutable checksums. |
| Native UI contract audit | `npm run audit:native-ui` passes 67 static accessibility/responsive contracts covering first-run connection setup, navigation/composer labels, model-library controls, project setup, compact toolbars, compact drawer focus, chat-history disclosure focus, settings-trigger restoration, model-library dialog return paths, workspace-switch focus, compact project-action focus, project-folder setup focus, project-file editing focus, terminal dialog focus, terminal input naming, terminal output labeling, terminal output keyboard reachability, terminal status announcements, GitHub review first-file focus, GitHub action completion focus, GitHub sign-in/repository focus and canceled-sign-in recovery, GitHub clone readiness/progress, GitHub clone failure recovery, GitHub repository inline validation and polite error announcements, GitHub clone destination conflict preflight, command-review focus, command-review outcome focus, and command-result accessibility. Native CI and the release native job run this gate. | Add live Narrator/keyboard and Linux/Skia interaction smoke from packaged artifacts; static contracts cannot prove runtime focus behavior. |
| Portable artifact reproducibility | Two independent local `dotnet publish -c Release -f net10.0-desktop -r win-x64 -p:SelfContained=true` runs produced 281 files each; every filename, byte length, and SHA-256 matched (281/281). | Re-run on clean CI runners from a tagged source commit and publish the resulting checksums through a trusted release channel. |
| Portable data retention separation | Disposable filesystem smoke placed four profile files outside two extracted app directories, replaced the app directory, and observed all four profile hashes unchanged. | Repeat with a launched clean portable artifact, real resolved Windows/Linux application-data paths, an upgrade, interrupted replacement, and explicit uninstall/data-removal behavior. |
| Windows runtime | Windows 11 unpackaged and isolated temporary-profile smoke runs have opened the native app; branch navigation and contextual Fork are verified against disposable data. | Run the packaged artifact through clean install, upgrade, uninstall, data-retention, and rollback checks. Verify keyboard focus and narrow layouts from the packaged build. |
| Linux runtime | Ubuntu 24.04 WSL2/WSLg build and initial launch evidence exists; WSL portal/EWMH warnings remain. The corrected `linux-x64` self-contained publish pins `Svg.Skia 4.9.1` to Uno 6.7's SkiaSharp 3.119.2 ABI and produced 306 files (175,768,323 bytes). Under disposable HOME/XDG/application-data paths, the published process remained alive for an 8-second WSLg smoke window without a startup crash; only disposable profile/Mesa cache locations were used. | Run the packaged artifact on a real Ubuntu desktop plus Debian, Arch, and Fedora; record native-library prerequisites and clean launch/exit behavior. This WSLg check is initial runtime evidence, not distro acceptance. |
| Electron history migration | Isolated Playwright export tests and Core importer tests prove chats-only export, secret/path exclusion, bounded parsing, metadata preservation, no-write planning, idempotent re-import, and fork/archive independence. | Use a user-provided real Electron export on Windows and Linux; verify native picker preview, Cancel, confirm, restart, duplicate protection, and edited-chat preservation. The native picker/focus UI automation path is currently unstable. |
| Signing and recovery | No signing identity, installer, auto-update, rollback mechanism, or reproducible-build attestation is configured. | Choose signing/release credentials, produce trusted installers, define rollback, and verify checksums/signatures from the exact published artifacts. |

Local verification on 2026-11-05 (Windows 11 Pro, x64, .NET SDK 10.0.401):

- `dotnet publish native/ServerRock.Native/ServerRock.Native.csproj -c Release -f net10.0-desktop -r win-x64 -p:SelfContained=true --output <temp>` succeeded.
- The equivalent `linux-x64` publish from the Windows host also succeeded. This verifies cross-RID compilation/publish only; it is not by itself Linux runtime evidence.
- Two independent `win-x64` self-contained publishes from the same checkout produced 281 files each with identical filename, length, and SHA-256 manifests. This is local deterministic-output evidence, not a signed or independently reproducible release attestation.
- After aligning `Svg.Skia 4.9.1` with Uno 6.7's SkiaSharp 3.119.2 ABI, the current checkout published a self-contained `linux-x64` artifact with 306 files (175,768,323 bytes), including `ServerRock.Native`. A disposable Ubuntu 24.04 WSLg run kept that artifact alive for 8 seconds without a startup crash. This is initial WSLg smoke evidence, not full Linux distro acceptance.
- The first publish attempt followed the Uno guide's additional `-p:TargetFrameworks=net10.0-desktop` argument. In this multi-project solution that global property also overrode `ServerRock.Core`'s `net10.0` target and failed with `NETSDK1139`. The working command omits that override. Keep this behavior under review if the project target structure changes.
- Existing native CI runs on Ubuntu 24.04 and Windows 2022; it builds the shared Skia target and runs Core checks. CI is not currently a package installation or GUI acceptance test.

## Platform prerequisites and limits

- Windows portable archive is x64 and self-contained for .NET. It is unsigned and has no installer/uninstaller. Windows 11 build/test evidence is recorded in `docs/NATIVE_MIGRATION.md`; it does not establish Windows 8/8.1 compatibility. Windows 10 support must be verified against specific editions/builds and actual host runs.
- Linux archive is x64 and self-contained for .NET, but still depends on native desktop libraries. Uno's [Skia Desktop requirements](https://platform.uno/docs/articles/features/using-skia-desktop.html) currently list X11/graphics/font dependencies, including Mesa/OpenGL, DBus, fontconfig, XRandR, and XI libraries. The published archive is not yet a DEB, RPM, or AppImage, and the workflow has not verified those dependencies on Debian, Ubuntu desktop, Arch, or Fedora.
- No ARM64 artifacts are produced. No code-signing identity, reproducible-build attestation, rollback mechanism, or auto-updater is configured.
- Unsigned archives and their checksum file provide transport integrity checking only when the checksum itself is obtained through a trusted channel. They do not prove publisher identity.

## Data retention contract to verify

Native stores user data outside the application publish directory, under the platform's .NET application-data locations, in a `ServerRock/AI` directory. Conversations, settings, projects, and undo history use files such as `conversations.json`, `settings.json`, `projects.json`, and `workspace-undo.json`; GitHub credentials are stored separately using the platform credential mechanism. Replacing an extracted app directory should therefore leave those stores intact. The present portable builds have no uninstall action that deletes app data. The native chat importer leaves its selected source file and current history unchanged, adds only reviewed conversation history, and records import identity with each migrated chat so repeat imports do not overwrite later user edits.

Before release, record actual resolved paths from clean Windows and Linux profiles, then verify:

1. Install/extract a clean build and launch it with a local model endpoint configured.
2. Create a conversation, project, setting, and guarded workspace edit; close the app normally.
3. Upgrade by extracting the next build over a fresh app directory while leaving profile data intact; launch and confirm each record remains available.
4. Test interrupted extraction/replacement and document recovery. Do not call this atomic upgrade until verified.
5. Remove only app binaries and confirm profile data is retained. Test any future explicit “remove data” option separately with clear confirmation.
6. With a user-provided Electron JSON history export, test the native file picker on Windows and Linux: inspect the preview, Cancel and confirm history/source are unchanged, then import and restart. Reselect the file and confirm no duplicates appear; edit an imported conversation and repeat to verify the edit is preserved. The current repository fixture proves the JSON shape and Core round-trip, but is not an export extracted from a real Electron profile.
7. Verify each downloaded archive using its adjacent `SHA256SUMS.txt` (`Get-FileHash` on Windows or `sha256sum -c SHA256SUMS.txt` on Linux).
8. Launch and smoke-test on every claimed OS/distro/package combination, record runtime dependencies and failures, then repeat from the exact release artifact.

## Release blockers

Do not retire the Electron distribution or describe native builds as production installers until the migration capability map and the checks above are complete. The current workflow is a reviewable artifact handoff, not the final release path. Remaining blockers include real desktop install/upgrade runs, Linux distro coverage, Windows-version coverage, signing, installer/uninstaller behavior, a user-facing Electron export handoff and real-profile cross-app round-trip, keyboard/narrow-layout and Linux/Skia interaction acceptance, and an explicit rollback strategy. Archive/fork/branch metadata preservation and native navigation/fork behavior are covered by the importer, Core regression suite, native builds, and isolated Windows smoke; cross-platform and accessibility focus acceptance still needs completion. See `docs/NATIVE_MIGRATION.md` for feature parity and platform status.
