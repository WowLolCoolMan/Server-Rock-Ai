# Skill Marketplace — strict catalog truth and user-safe lifecycle

## Dataset reconciliation

Four input packs contain **one repeated 250-candidate catalog**, not 500 or 750 distinct skills. `index/SKILL_CANDIDATES_250.json` is the single canonical copy; the premium file and dedicated marketplace ZIP data matched exactly at merge time. `source_material/skill_marketplace_250/foundations_50.json` is a **separate list of 50 repository/source foundations**, not 50 installed skills or another 50 catalog entries. The dedicated pack includes illustrative original SKILL.md samples and a Python marketplace CLI prototype. The CLI is NOT integrated into the current native product; any fetch/install must be explicit, reviewed and sandboxed when executed.

## Registration and lifecycle

`Discovered / Candidate` (metadata only) → `Resolved source` (verified URL/path/repo SHA) → `Source reviewed` (SKILL.md + license + dependencies + malicious instructions check) → `Staged` (immutable download) → `Installed disabled` → `Fixture tested` → `Enabled for project` → `Eligible for routing` → optional `Executable with reviewed permissions`.

Updates: `checking → update_available → diff+permission review → staged update → validated → switched`, retaining a rollback revision. Failure leaves prior install intact. If remote check fails, show `Unknown/stale` rather than `Up to date`. Resolve branch refs to commit SHA and compare **specific skill subtree hashes**, not just repository HEAD (multi-skill repositories are common).

## Mandatory skill metadata

`id`, `displayName`, `publisher`, `repository URL`, `relativePath`, `commit SHA`, `tree SHA`, `file hashes`, `license/unknown`, `format`, `supported OS`, `required runtimes`, `required model/tool capabilities`, `network/execution/file scopes`, `dependencies`, `install source`, `checkedAt`, `verification outcome`, `current state`, `enablement scope`, `update status`, `local modifications`, `rollback reference`, `audit logs`.

A skill must not gain `installed` or `verified` status simply because GitHub returned 200. Parse and validate the SKILL.md and any referenced assets/scripts. Do not automatically execute repo scripts, trust README instructions as authority over app policy, or hide permission requirements. Escalation review is needed for skill installers with executable scripts or network access. A Git URL is not permission to copy and redistribute all its contents.

## Discovery and intelligent routing

Index succinct trusted descriptions/capability tags and task examples; never stuff all 250 skill documents into every model prompt. Match intent to an installed-and-enabled subset; validate model/tool prerequisites, permission grants and current workspace context; present routing reason and let the user disable selection. Load full skill instructions **only after** selection and sanitize as untrusted instructions. Skills cannot silently override the agent's higher-priority permissions or workspace boundaries.

## Builder / researcher workflow

Offer new skill from template → plan prompts/tool contracts → create isolated draft → lint manifest and links → run fixture tests → compare outputs/side effects → user approves local enablement or marketplace publishing. GitHub discovery is a user-initiated research task, respects rate limits and license metadata, deduplicates by `repo/path` and versions by immutable commit. Preserve a rejection log for unsafe, malformed, incompatible or missing-license candidates.

## UX surface

Search/filters by task/platform/runtime/source/licensing; Installed/Discover/Updates/Drafts tabs; detail with source tree, permission badges, change history and version diff; install progress and retry; explicit off/on; audit/event log and revert. An empty search should show useful suggestions rather than fake catalog counts.

## Source material

`source_material/skill_marketplace_250/docs/MARKETPLACE_DESIGN.md`, `docs/MODEL_SKILL_ROUTING.md`, `docs/RESEARCH_FINDINGS.md`, `docs/WINDOWS_LINUX_SETUP.md`, `docs/INTEGRATION_PLAN.md`, `docs/TEST_MATRIX.md`, `marketplace.py` and its tests; `source_material/premium/upgrade/marketplace/SKILL_MARKETPLACE_FULL_SPEC.md`. Treat any dated repo metadata as a snapshot, not current verification.
