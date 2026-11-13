# Cross-platform Agent Skill Marketplace: Architecture and Update Flags

## Critical distinction

**Catalog** = metadata/link, **Installed** = files fetched to user-data folder, **Enabled** = permitted to be selected for tasks, **Executable** = scripts allowed through reviewed tool policy. Never use one “installed” boolean for all four states. Third-party skills may include arbitrary scripts, network dependencies or instructions that do not fit every local/cloud model.

## Marketplace data model

```
skill_id, publisher, repo_full_name, branch_or_release, relative_path,
source_url, license, readme, description, categories, supported_os,
required_tools, required_model_capabilities, minimum_runtime_versions,
remote_commit_sha, remote_skill_tree_sha, installed_skill_tree_sha,
installed_files_sha256, installed_at, enabled, verification_status,
permission_grants, update_status, last_checked_at, error, local_edits
```

Use immutable source SHA for version pinning; semver from YAML is informational only. A repo commit can change without affecting a particular skill. Resolve `git rev-parse HEAD:<relative/path>` after fetching the remote branch and compare **tree hashes**. `HEAD` alone is unsuitable for a monorepo catalog. If GitHub API is used, retrieve the Git Trees API object for the exact subtree. Rate-limit/cache aggressively; use ETag/conditional requests where appropriate. `git ls-remote` alone cannot determine subtree changes. Do not mark “Current” on any failed check.

## Marketplace status machine

```
Not installed -> Fetching -> Scanning -> Review required -> Installed disabled
Installed disabled -> Enabled by user -> Active/Ready
Installed -> Update available -> Preview diff/permissions -> Staging -> Updated
                                                   \-> Error -> Rolled back
Installed -> Local edits -> Conflict (review/keep local/backup)
Any check -> Network unavailable / Rate limited / Auth required / Source removed
```

**Update badges:** display only a meaningful single indicator on each affected skill; do not badge all 250 because one repo HEAD changed. Use checked-at timestamp and `Check updates` action. If user explicitly opts in to periodic checks later, disclose GitHub network access and bandwidth first. Core updater and skill updater must be separate services and signing/trust policies.

## GitHub ingestion: Linux and Windows

Preferred native path: use `HttpClient` to GitHub API with source repo/branch metadata and scoped source tar/zip checks; validate each archive entry's parent/symlink properties and SHA. Alternative is installed Git with `git clone --depth=1 --filter=blob:none --sparse`; use `git sparse-checkout set <directory>` before copying skill payload. The prototype `marketplace.py` implements this alternative. On Windows, use Git for Windows (`git.exe` found on PATH), do not call `sh` or rely on WSL. On Linux, use `/usr/bin/git` or PATH-resolved git. Run subprocess with argument arrays, **not** shell interpolation.

**Default store paths** (do not write into `Program Files` or bundled artifact):

- Windows `%LOCALAPPDATA%\ServerRock\AI\AgentSkills\installed\<number>\`
- Linux `${XDG_DATA_HOME:-~/.local/share}/server-rock-ai/agent-skills/installed/<number>/`

The native app should use its own platform app-data resolution if it differs from prototype; migrate or import existing skills carefully. Store metadata separately and atomically. Install files with user-owned permissions, no executable bit until user grants permission, no symlinks or traversal, bounded bytes/files, and no package postinstall hooks. Use quarantine staging and review license and `SKILL.md` before Enable.

**Permissions:** showing a third-party SKILL.md in the UI is not granting access to tool invocation. Scripts require a sandbox or OS-level restrictions and explicit per-category approvals (filesystem paths, shell, network hosts, GitHub, project edit, credentials). A text prompt, skill YAML or tool result must never produce host permission without the user.

## Example check-updates logic

1. Load installed lockfile: repo, relative path, installed commit, **installed subtree SHA**, SHA-256 per installed file.
2. Group installed skills by repository; one network fetch per repo, bounded parallelism and backoff.
3. Resolve the remote default branch/revision; compute exact remote subtree SHA for each installed relative path.
4. If fetch fails: `check-failed`, preserve prior status but show stale timestamp. If folder removed: `source-removed`.
5. If remote tree == installed tree: `up-to-date`. Else if locally modified: `conflict`. Else `update-available`.
6. On Update click: fetch in staging, scan file additions/removals, show diffs/permissions/license changes, obtain explicit approval, run validation.
7. Atomically swap installation, retain prior tree for rollback, update lockfile, and show success **only after** hash and policy checks pass.

## UI card layout

`[icon] Name | Category | Installed version | Publisher` plus short description and source link; show model/tool/OS compatibility row. Right-side details: Overview, Readme, Permissions, Dependencies, Changelog, Source files, Reviews, Install/Update. Search and filters on top, activity footer with last-check timestamp. Avoid action buttons under modals or behind-page. Give model-incompatible skills a clear reason and manual alternative.

## Quality/scoring and GitHub skill scout

Discovery must not accept stars as a guarantee of quality. Collect repository URL, exact SKILL.md path, commit, license, last change, maintainer activity, official/independent source, compatibility metadata, scripts, test presence, dependency footprint, known risks, documentation quality, and duplication against installed capabilities. Score with confidence and allow user to review. Network discovery should be user-invoked, or separately opted-in with a declared cadence; offline mode remains usable.

## Integrity and threat-model checklist

- No `git clone` from arbitrary host; require github.com HTTPS or validated allowlist and prevent credential embedded URLs.
- Never execute Git hooks/submodules, Git LFS smudge commands or arbitrary installer scripts. Test malicious submodules and smudge filters.
- Prevent archive zip-slip, symlink escape, case-insensitive path collision on Windows, Unicode normalization collisions, Windows reserved names, junction/reparse tricks and file races.
- Check source license (upstream licenses may differ by subfolder) and new dependencies before enabling.
- Scan for prompt-injection content and secrets/exfiltration instructions; report suspicious findings. Scanning does not make code safe.
- Isolate models, tools and workspace permissions. Never give a downloaded skill all CLI permissions.
- Verify cancellation atomicity, backup rotation and local customization preservation.
- Limit GitHub response sizes, redirects, clones, rate-limit handling, errors, retries and concurrent fetches.
- License and provenance labels cannot imply certification unless your review process actually certifies them.

## Prototype limitations

`marketplace.py` is a **reference**, not hardened production installer: no OS sandbox, license parser, signature verifier, dependency resolver, parallel installer locks or enterprise trust store. It intentionally does not execute imported scripts. Use its testable logic as a starting point, then implement .NET native services, process isolation, and an independent security review before public release.
