# Security, release, storage, and Git specification

## Immediate code-level fixes

### Windows launcher

Current `packaging/native/install.ps1` creates a version root and `current` junction, then copies only executable into `$InstallRoot/ServerRock.Native.exe` and shortcuts to that copy. That is not the same as launching published multi-file app beside its .NET files. Change shortcut target to `$InstallRoot/current/ServerRock.Native.exe`; set working directory to the versioned/current directory; ensure Start Menu paths are correctly quoted. Rollback should switch `current` and not copy a detached launcher. If using a stable shim, use an actual executable launcher that starts the versioned path and validates its location, not a bare copy of the app binary.

### Git selected commits

`GitHubWorkspaceService.CommitSelectedCoreAsync` uses the normal index and `git commit -m`. If unrelated staged changes exist, they will be committed too. Construct isolated temporary index using `GIT_INDEX_FILE` under private temp, `git read-tree HEAD`, stage exact reviewed paths while bound to immutable content hashes; commit with this index and safely integrate commit while respecting branch movement. Simpler interim alternative: refuse commit whenever normal index contains any unselected stage, and explicitly show that content; the full fix should preserve partially staged diffs and index. Never use `git commit --amend`, `reset`, force-push, broad stage, or clean to work around this.

### UI audit

`audit-native-ui.cjs` uses exact multiline string matching; Windows checkout line endings can differ. Normalize to LF before comparisons, add a test proving CRLF behavior, and migrate to runtime UI acceptance, not source-string ceremonies. Current Windows job fail is real; root-cause needs confirmation.

## Policy architecture

Enforce allowed effect by tool action, exact file or repository, actor/request id, source trust and permission expiry. A user-supplied file, model output, skill, web result or command output cannot increase this set. Approvals are one-use and tied to exact action argument digest. Keep audit logs redacted for credentials and personal data. Do not send local project files to cloud during fallback without consent. Web content and remote model text are untrusted, even inside a known skill.

Design an isolated executor for agent-proposed shell commands; user-approved commands may remain unrestricted with explicit warning if that is intentional, but do not claim containment. Windows: consider AppContainer/low-privilege process + job objects or documented supported sandbox technology. Linux: separate unprivileged account, namespaces/bubblewrap/container and explicit mounts; portability and distribution must be tested. No generic `ProcessStartInfo.WorkingDirectory` is a filesystem restriction.

## Release gates

1. Main branch rules: PR review, required Windows/Linux CI, signed commits/releases as appropriate, CodeQL, dependency review and least-privilege workflow token; pin actions to audited SHA.
2. Build exact tagged artifact with reproducible dependencies. Run startup/window smoke, real installer and rollback test, clean profile, update/corruption backup scenario.
3. Sign Windows EXE/package with Authenticode and Linux archive/manifest as supported. Verify signatures and checksums in client using a pinned trust root; checksums alone do not authenticate provenance.
4. `release-manifest.json` should specify version/channel/platform, immutable download location, content digest, signing identity, minimum schema version and staged-migration/rollback rules.
5. State retention: install root is not data root; uninstall leaves history unless explicit confirmed deletion; migration is atomic with a backup; warning for schema downgrade.
6. Keep feature matrix IMPLEMENTED/PARTIAL/PLANNED in docs and acceptance tests. Do not label placeholders available or describe native code as integrated if not.

## Test plan

- Git: untracked, already staged, partially staged, rename, unicode path, external symlink, denied auth, remote advances between fetch and push, cancellation and concurrency.
- Installer: no dotnet SDK, clean Win11, normal user rights, Start Menu, update/rollback/uninstall and orphaned files; test Linux shell installer link and launcher after version switch.
- Credentials: tokens never in argv/URLs or diagnostics; remote redirects cannot forward auth; self-hosted endpoint restrictions; credential vault rotation and revoke.
- Storage: truncated/corrupt JSON, old schema, lock failure, disk full, thread race, interrupted atomic write, encrypted data import/export.
- Supply-chain: malicious zip slip, symlink, too-large repo, parser bombs, dependency scripts, update privilege creep, publisher rename, offline behavior.
