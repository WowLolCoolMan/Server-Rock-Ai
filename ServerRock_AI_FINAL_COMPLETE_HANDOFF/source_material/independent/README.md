# Server Rock AI — Complete Remediation & UI Delivery Handoff

**Audit baseline:** Server Rock `4ad39cfb89ddf97cd1ddd4533c4b879d8124ac95` (2026-11-08).

**Primary source:** [Server Rock](https://github.com/WowLolCoolMan/Server-Rock-Ai). Inspiration comes only from high-level user-facing ideas in `inspiration/`.

This ZIP is a **technical implementation plan, architecture and UX reference package, tests/checks, and agent handoff**, **not a patched application**. Findings are marked as confirmed source, observed CI, risks that require reproduction, or product gaps. Rebase findings against the current commit before editing. Don't mistake a proposed design for shipped code.

## Start here

1. Open `00-START-HERE/EXECUTION_ORDER.md` and `00-START-HERE/CODEX_MASTER_PROMPT.md` in the checked-out Server Rock workspace.
2. Keep user files and Git history unchanged; create feature branches. Do not automatically clean/reset tracked or untracked work.
3. Run `tools/repo_preflight.py --repo <path>` or `tools/repo_preflight.ps1` on Windows; this only inspects.
4. Work through `backlog/issues.json` or `backlog/issues.csv` in P0 → P1 → P2 order, producing PR-sized checkpoints.
5. For architecture, open `architecture/AGENT_RUNTIME.md`, `architecture/MODELS_AND_MARKETPLACE.md`, and `inspiration/READ_THIS_FIRST.md` and `inspiration/26_IDEAS_NOT_IMPLEMENTATIONS.md`.
6. For UI, read **all** `ui/` documents. The overlay/focus/lifecycle work is first, not a cosmetic task.
7. Run `tools/validate_handoff.py` on the ZIP's extracted folder; then run target repository tests on each change.

## Essential verified observations

- GitHub Native CI run `42403777254`: Ubuntu passed and Windows failed at native UI audit, with 11 exact-source-text mismatches. CRLF sensitivity is a *hypothesis* needing verification, not the final diagnosis.
- The Windows installer places a copied EXE alone at the install root; the versioned publish is multi-file.
- GitHub `CommitSelectedCoreAsync` uses bare `git commit -m`, potentially capturing unrelated staged changes.
- Coding Agent is single model response → XML-ish proposed changes/commands → approval; no durable autonomous agent loop.
- Skills and plugins are static catalogs; scheduled tasks are a placeholder.
- Default main branch protection: false; no GitHub Releases surfaced at audit time.
- MainPage root has drawer backdrop and companion overlay; feature screens are heavy ContentDialogs, not stable route pages.

## What this package contains

A 40-issue structured backlog, file-specific fixes with acceptance conditions, agent runtime design, security and release guidance, detailed UI system and page specifications, overlay lifecycle rules, testing matrix, 26 independent product inspiration ideas (no source mappings), and runnable read-only inspection and handoff validation tools. Also includes an illustrative **non-production** WPF/Uno-style XAML layout skeleton and interaction state machine pseudocode; adapt to the actual Uno version before shipping.

## Strict ideas-only policy

Odysseus is **not being incorporated** into Server Rock. It was looked at solely as a prompt for **what users could benefit from**. No integration, code, translation, borrowed UI components, copied assets, styling or architecture, packages, build links, or dependency is authorized by this handoff. All implementations are fresh Server Rock C#/Uno work defined by Server Rock's requirements and tests. See `inspiration/READ_THIS_FIRST.md`.

**Package boundary:** 40 issue fixes + 26 original feature concepts + UI acceptance contracts + inspection scripts; no patched app, no imported third-party source and no guarantee that tests were executed on either desktop platform.
