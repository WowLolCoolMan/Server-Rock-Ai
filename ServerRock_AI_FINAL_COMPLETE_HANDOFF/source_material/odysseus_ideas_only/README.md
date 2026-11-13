# Server Rock AI — Complete Remediation & UI Delivery Handoff

**Audit baseline:** Server Rock `4ad39cfb89ddf97cd1ddd4533c4b879d8124ac95` (2026-11-08); Odysseus reference default `dev`, tree `34e4666f357d458eb31de21b6badf52ce2ef7a96`.

**Repositories**: [Server Rock](https://github.com/WowLolCoolMan/Server-Rock-Ai) · [Odysseus](https://github.com/odysseus-dev/odysseus).

This ZIP is a **technical implementation plan, architecture and UX reference package, tests/checks, and agent handoff**, **not a patched application**. Findings are marked as confirmed source, observed CI, risks that require reproduction, or product gaps. Rebase findings against the current commit before editing. Don't mistake a proposed design for shipped code.

## Start here

1. Open `00-START-HERE/EXECUTION_ORDER.md` and `00-START-HERE/CODEX_MASTER_PROMPT.md` in the checked-out Server Rock workspace.
2. Keep user files and Git history unchanged; create feature branches. Do not automatically clean/reset tracked or untracked work.
3. Run `tools/repo_preflight.py --repo <path>` or `tools/repo_preflight.ps1` on Windows; this only inspects.
4. Work through `backlog/issues.json` or `backlog/issues.csv` in P0 → P1 → P2 order, producing PR-sized checkpoints.
5. For architecture, open `architecture/AGENT_RUNTIME.md`, `architecture/MODELS_AND_MARKETPLACE.md`, and `references/ODYSSEUS_MAPPING.md`.
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

A 40-issue structured backlog, file-specific fixes with acceptance conditions, agent runtime design, security and release guidance, detailed UI system and page specifications, overlay lifecycle rules, testing matrix, Odysseus design reference mapping, and runnable read-only inspection and handoff validation tools. Also includes an illustrative **non-production** WPF/Uno-style XAML layout skeleton and interaction state machine pseudocode; adapt to the actual Uno version before shipping.

## Licensing

Odysseus is AGPL-3.0-or-later. This package **does not redistribute its source or images**. Its source is referenced for study, architecture and behavior. **Do not copy Odysseus code into Server Rock without a deliberate license/compatibility review** and appropriate obligations. Reimplement patterns cleanly in C#/Uno when possible. The referenced branches are mutable; pin exact commits when comparing code.
