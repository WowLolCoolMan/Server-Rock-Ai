# Server Rock: AI — Agent Skill Marketplace Blueprint & 250-Skill Catalog

**Target:** [WowLolCoolMan/Server-Rock-Ai](https://github.com/WowLolCoolMan/Server-Rock-Ai), native Uno Platform + .NET 10 desktop, Windows/Linux.

**This ZIP is a research, architecture, tooling, and developer-handoff bundle, NOT a modified Server Rock binary or pre-installed third-party skills.** No existing app source files were changed. The files here describe what to implement and include a working prototype marketplace CLI to test GitHub fetch, installation, update detection and rollback safely. Integration into the native UI still requires code changes and testing.

## Contents

- `catalog_250.json` and `CATALOG_250.md`: 250 unique GitHub skill locations, indexed by source/path. Some are provider-specific, and must be assessed for SKILL.md, license, runtime tools, and Windows/Linux behavior at install time.
- `foundations_50.json`: 50 related source repositories and skillbases; **not** 50 additional individual skills.
- `docs/RESEARCH_FINDINGS.md`: source verification and constraints, including the three requested skills.
- `docs/SERVER_ROCK_RELEASE_READINESS.md`: comprehensive prioritized native product remediation plan, evidence and acceptance criteria.
- `docs/INTEGRATION_PLAN.md`: exact Server Rock project files and implementation milestones.
- `docs/MARKETPLACE_DESIGN.md`: installation, updates, UX, provenance, API, security and caching.
- `docs/MODEL_SKILL_ROUTING.md`: how models find skills, create missing skills, and avoid context overload.
- `docs/WINDOWS_LINUX_SETUP.md`: operator/user installation and usage commands.
- `docs/TEST_MATRIX.md`: cross-platform verification matrix and no-false-success rules.
- `marketplace.py`: cross-platform, **user-initiated** GitHub skill inspect/install/update CLI prototype using Git and the 250-skill manifest.
- `tests/test_marketplace.py`: offline tests for manifest, path safety, content fingerprints.
- `original_skills/`: four original skill templates implementing skill routing, creation, research, and universal problem solving.

## Quick start

Windows PowerShell: `py -3 marketplace.py catalog --query skill-creator`

Linux: `python3 marketplace.py catalog --query skill-creator`

Install a chosen skill (requires Git installed and network access):

`python3 marketplace.py install 019 --approve`

Then: `python3 marketplace.py status` and `python3 marketplace.py check-updates`.

**Important:** This prototype fetches third-party Markdown/scripts into a private user-data folder but never runs those scripts. `--approve` is an explicit user action authorizing the clone and copy. The native UI must add security review and per-capability consent before executing installed tools. It must not silently auto-update them.

## Status of this research

- Verified 250 unique names/paths against public GitHub directory indexes or search results at collection time; **not** 250 independently executed/benchmarked skills.
- Server Rock audit is based on selected public repository files and its own release documents as of 2026-11-08; **not** a complete codebase static analysis, successful native build, accessibility session, penetration test, or packaged installation.
- Changes in upstream GitHub repos after the catalog snapshot can invalidate links, folder names or dependencies. The marketplace detects that at install time.
- Review each upstream license before redistributing code; this ZIP vendors **no** third-party skill code.

Recommended order: `docs/SERVER_ROCK_RELEASE_READINESS.md` -> `docs/INTEGRATION_PLAN.md` -> `docs/MARKETPLACE_DESIGN.md` -> `docs/WINDOWS_LINUX_SETUP.md`.
