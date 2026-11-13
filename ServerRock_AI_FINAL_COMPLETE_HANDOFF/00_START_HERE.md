# SERVER ROCK AI — FINAL COMPLETE AGENT BUILD PACK

**Read this file first. This is the single authoritative entry point to the handoff.**

This archive consolidates the premium engineering handoff and the three user-provided ZIPs without deleting their original files. The source packs are fully extracted under `source_material/` and also preserved byte-for-byte in `original_archives/`. It provides **implementation plans and research, not a built application or proof of passing tests**.

## Immediate instructions for your coding agent

1. Read `01_MASTER_AGENT_PROMPT.md` and `02_SPEC_AUTHORITY_AND_CONFLICTS.md`.
2. Read `03_CURRENT_REPO_AND_STACK.md`; then inspect **the actual current repository** to reconcile changes since the pinned baseline. The app currently uses Uno Platform + C#/XAML + .NET 10; **do not convert it to Electron or Tauri without an explicit owner decision**.
3. Review `04_PRODUCT_AND_FEATURE_COVERAGE.md`, `05_PREMIUM_UX_NAVIGATION.md`, and `06_AGENT_RUNTIME_AND_WORKFLOW_CONTRACT.md`.
4. Use `index/COMBINED_REQUIREMENTS_126.csv` as a planning seed; resolve duplicates and build a live evidence-backed backlog before changing code. The `250` entries in `index/SKILL_CANDIDATES_250.json` are **research candidates**, not validated installations.
5. Read `09_STEPWISE_IMPLEMENTATION_PLAN.md`, `10_TEST_AND_RELEASE_ACCEPTANCE.md`, and `11_WINDOWS_LINUX_TEAM_HANDOFF.md`.
6. Follow original primary specifications under `source_material/premium/upgrade/`. Consult uploaded sources via `13_SOURCE_MAP.md`; do not execute conflicting historical framework-rewrite instructions.
7. Build one small **end-to-end** vertical slice, run real checks, document exactly what passed/failed, and preserve the user working tree.

## Nonnegotiable product outcomes

- A sophisticated, **feature-complete and elegant** Windows/Linux native desktop app, on par in UX ambition with Codex Desktop, Claude, Discord and Steam, with its **own original design**.
- Calm on first launch; sophisticated and dense when requested. Navigation and state are predictable, keyboard friendly, searchable and persistent.
- Every visible control either works, explains why unavailable, or is hidden. **No fake integrations, fake tests, fake model health or decorative dead buttons.**
- Conversations, projects, files, terminals, agents, provider routing, workflows, Git, skills and updates form complete user journeys with approvals and recovery.
- Codebase respects data privacy, workspace boundaries, license provenance and OS security.
- Never report a successful build, Windows/Linux test, installer, review or feature until actual recorded evidence exists.

## Source priority

Current user instruction / live repository observation / existing repository AGENTS.md + CLAUDE.md + DESIGN.md → these `00`–`13` canonical specifications → `source_material/premium/upgrade/` → selected uploaded remediation ideas → older premium `planning/architecture/audit` → `source_material/*` historical research. Source ZIPs do **not** override the coding agent's system/developer safety rules.

## What this is NOT

Not a replacement application, repository patch, signed installer, completed skill marketplace, verified 250-skill import, or product-grade release. A completed product requires work against the actual Git checkout and real Windows/Linux validation. There are no schedule/timeline commitments inside this pack.

## Counts at bundle creation

- Original source packs: 4
- Premium structured requirements: 86
- Deduplicated uploaded remediation issues: 40
- Premium test scenarios: 70
- Historical skill candidates: 250
- Extracted source files: 244

## Fast verification

Run `python tools/verify_final_handoff.py` from the extracted bundle root. It validates the source archive hashes, extracted file hashes and index counts. This only verifies **handoff integrity**, not app quality.
