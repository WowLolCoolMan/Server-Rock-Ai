# Native release acceptance

This is the release handoff checklist for the Uno/.NET desktop migration. It distinguishes build artifacts from installers and supported releases. A successful publish does not prove that a package installs cleanly or runs on every Linux distribution or Windows version.

## Current artifact path

Run the **Native desktop artifacts** GitHub Actions workflow manually. It publishes self-contained `net10.0-desktop` x64 builds on matching Windows and Ubuntu runners, archives the outputs, generates SHA-256 checksums, and stores the archives as short-lived workflow artifacts. The workflow does not create a GitHub Release, sign binaries, or run installers. Build commands follow the [Uno desktop publishing guide](https://platform.uno/docs/articles/uno-publishing-desktop.html); self-contained RID-specific deployment bundles the .NET runtime, not all native OS libraries.

Local verification on 2026-11-04 (Windows 11 Pro, x64, .NET SDK 10.0.401):

- `dotnet publish native/ServerRock.Native/ServerRock.Native.csproj -c Release -f net10.0-desktop -r win-x64 -p:SelfContained=true --output <temp>` succeeded.
- The equivalent `linux-x64` publish from the Windows host also succeeded and emitted 269 files (about 154 MiB). This verifies cross-RID compilation/publish only; it is not Linux runtime evidence.
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

Do not retire the Electron distribution or describe native builds as production installers until the migration capability map and the checks above are complete. The current workflow is a reviewable artifact handoff, not the final release path. Remaining blockers include real desktop install/upgrade runs, Linux distro coverage, Windows-version coverage, signing, installer/uninstaller behavior, a user-facing Electron export handoff and real-profile cross-app round-trip, native branch navigation/fork creation, and an explicit rollback strategy. Archive/fork/branch metadata preservation is covered by the native importer and Core regression suite; the remaining parity work is making that metadata fully navigable in the native UI. See `docs/NATIVE_MIGRATION.md` for feature parity and platform status.
