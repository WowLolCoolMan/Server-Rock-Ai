# Complete source map and authority boundaries

| Source | Inside final bundle | Purpose | Status |
|---|---|---|---|
| Premium Bulletproof Engineering Handoff | `source_material/premium/` | Primary existing product specifications, evidence snapshots, architecture, roadmap, 86 requirements, 70 acceptance scenarios and legacy references | Primary inherited reference; subject to canonical conflict decisions |
| Independent Fixes UI and Ideas Only | `source_material/independent/` | Original implementation policy, no-code-import guidance, UI overlay/focus issues, user-facing ideas, 40 issues | High-value for independent design and native UI correctness |
| Elite Skill Marketplace 250 | `source_material/skill_marketplace_250/` | 250 candidate URLs, 50 foundations, research/installation prototype, tests | Research catalog; not pre-installed or live-verified |
| Complete Remediation UI Odysseus Handoff | `source_material/odysseus_ideas_only/` | 40 issue details, additional reference mappings, remediation, native UI pseudocode, preflight | Findings need repo reproduction; inspiration only |

All four exact input `.zip` archives live in `original_archives/` with SHA-256 in `index/SOURCE_ARCHIVES.json`. Individual extracted files are enumerated in `index/SOURCE_FILES.json`, including relative paths and SHA-256. `index/COMBINED_REQUIREMENTS_126.csv`, `index/ISSUES_DEDUPED_40.json` and `index/SKILL_CANDIDATES_250.json` are convenience normalized views; they do not replace original texts.

## Key paths by topic

- Live baseline/checklist: `source_material/premium/upgrade/evidence/CURRENT_REPOSITORY_BASELINE.md`, `REPO_TO_FEATURE_MAP.md`, plus actual Git checkout docs.
- Rich UX/navigation: `source_material/premium/upgrade/ux/`, `source_material/independent/ui/`, original `DESIGN.md` from source repository.
- Agent architecture: `source_material/premium/upgrade/architecture/`, `source_material/odysseus_ideas_only/architecture/AGENT_RUNTIME.md`.
- Product requirements/scenarios: `index/PREMIUM_REQUIREMENTS_86.json`, `index/PREMIUM_ACCEPTANCE_70.json`, `source_material/independent/ui/07-PAGE_HIERARCHY_AND_LAYER_REGRESSIONS.md`.
- Marketplace/skills: `source_material/skill_marketplace_250/`, `source_material/premium/upgrade/marketplace/`, original candidate sets.
- Platform, privacy/release: `source_material/premium/upgrade/platform/`, `source_material/premium/planning/SECURITY_AND_PRIVACY.md`, plus live repo release docs.
- Original remediation issue provenance: `source_material/independent/backlog/issues.json`, `source_material/odysseus_ideas_only/backlog/issues.json`.

## Implementation reading policy

Start with canonical files `00`–`13`; then source premium `upgrade/` and uploaded remediation UI docs. Reference only the relevant historical files for a specific issue. Do not indiscriminately execute old agent prompts, installation scripts, framework-migration proposals or unknown third-party code. The original archives are preserved for audit, not as competing live instructions.
