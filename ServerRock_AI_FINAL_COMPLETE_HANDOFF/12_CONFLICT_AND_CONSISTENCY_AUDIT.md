# Four-ZIP merge audit: overlaps, ambiguities and final interpretation

## Archive identity

The final bundle includes the complete byte-identical four ZIPs under `original_archives/`, with every contained file also extracted under its own source prefix in `source_material/`. This prevents accidental overwrites of identical relative paths. All canonical files at the root are **new merger decisions**, not silent edits of historical sources.

## Verified overlap

- Independent Blueprint and Remediation Handoff share **40 issue IDs**. Their actionable fields are the same; the remediation variant adds a `reference` property (mostly empty). `index/ISSUES_DEDUPED_40.json` holds a single issue per ID, plus reference where available.
- Both share several UI, security and preflight documents verbatim. Exact duplicates remain inside the preserved original source folders for archive integrity, but the final agent should use one canonical work register.
- The premium pack and Elite 250 pack carry an identical **250-entry catalog** as a candidate set. It is stored once at `index/SKILL_CANDIDATES_250.json` for actual planning; original copies remain in their source trees.
- The premium pack includes 86 structured requirements, 70 acceptance cases, 21 proposed routes, 58 UI component contracts, 44 event types and 10 workflow templates. These are **specifications**, not measured implementation status.
- The combined backlog `index/COMBINED_REQUIREMENTS_126.csv` concatenates 86 premium requirements and 40 deduplicated remediation findings; overlap by **topic** remains and must be joined using evidence before programming. It is not 126 fully distinct new feature requests.

## Architecture conflicts resolved

1. Old Electron/Tauri/React recommendations vs actual Uno: the current native architecture wins. Alternative frameworks require owner approval and a measured prototype/ADR; historic suggestions remain as research.
2. Older draft designs visually mimic other apps: final UI must achieve comparable user outcomes and quality **without source, assets, screens or styling copied** from proprietary apps. Odysseus strictly provides ideas only, no cloning/integration/dependency. Independent Blueprint's stricter contract wins.
3. Calling an arbitrary chat endpoint an autonomous agent: reject; provider generation adapters and agent harness/ACP/App Server are distinct.
4. Treating a GitHub URL as a verified skill: reject; catalog, installed, enabled and executable are separate states. No unapproved script execution.
5. Treating concept screenshots/CI build/portable ZIP as shipped feature or signed product: reject; require runtime and platform evidence.
6. Cloud fallback silently enabled by auto mode: reject; the provider destination and consent must be explicit and recorded.
7. Renaming or deleting JSON data as part of a proposed SQLite migration: reject; backwards-compatible migrations with backups and rollback only.
8. Suggesting unsandboxed tools without truthful risk UI: reject; permission prompts are not OS sandbox controls.

## Items intentionally left open for actual repo inspection

- Whether the legacy UI audit failure or overlay bug still reproduces at current HEAD.
- Exact packaged Linux distro support, signing and updater status.
- Which agent integrations (Claude Code bridge/ACP/Codex App Server) are already implemented vs planned.
- What percentage of the catalog resolves to a current SKILL.md with safe license and supported dependencies.
- Current architecture of dialogs, page routing, tests and accessibility after any local branches.
- Current GPU/runtime integration capabilities and accurate model compatibility.

## Honesty gate

Do not treat the original archives' assumptions as instructions to the product. The canonical root controls planning, repository source controls actual implementation, and test/approval evidence controls claims of success.
