# Skill Marketplace: premium, safe, reversible and transparent

## Scope
This app has an Agent Skill Marketplace, not a directory of Markdown links. Product target: 250–500 **curated candidates**, optional installation, capability-aware selection, human-visible sources and upgrade status. Never claim candidate count = installable/compatible/validated count. Included `upgrade/marketplace/catalog_250_candidates.json` preserves earlier 250-item research with historical evidence; it is not a trusted runtime registry. All rows need fresh URL, license, content hash and manifest inspection before installation.

## Product journeys
**Discover:** keyword/search/facets/categories, trusted source badges only when independently verified, recent/update indicators, compatibility (Windows/Linux, models, tools), clear install vs enabled status. **Detail:** publisher, repository URL/commit, license, source SKILL.md preview, required dependencies, hosts/file access, config examples, provenance, release history, unresolved warnings. **Install:** inspect → choose source/version → download to staging under app root (never arbitrary system path) → validate archive size/path/hash/license → run manifest parser without executing code → permission review → atomic enable. **Use:** installed + enabled in project + eligible + selected; show explanation in run trace. **Update:** compare version/source/diff/dependency permissions, notify only (default), request new grant for expanded privileges; snapshot rollback. **Uninstall:** show owned files/dependents, revoke grants, remove only app-owned installation, preserve user data unless separately confirmed.

## Metadata manifest proposal
`id` stable reverse-DNS, `name`, `description`, `version`, `publisher`, `source.repository`, `source.ref`, `source.commitSha`, `source.path`, `source.contentSha256`, `license.spdx`, `compatibility.os`, `compatibility.modelFamilies`, `compatibility.requiredCapabilities`, `entrypoints.instructions`, `requiredTools`, `requiredMcpServers`, `permissions.filesystem`, `permissions.networkHosts`, `permissions.execution`, `dependencies`, `configurationSchema`, `examples`, `updatePolicy`, `originVerifiedAt`. Unknown metadata remains visibly UNKNOWN, not pass.

## Skill selection/routing
1. Deterministic eligibility: installed, enabled, trusted enough for policy, compatible OS, dependency healthy, current model/agent capabilities, context window, permissions granted.
2. User explicitly pinned skills win within allowed scope. Otherwise index compact metadata and select at most 3–5 relevant skills. Do not load hundreds of full instruction files by default.
3. Let model propose skill relevance; trusted router performs policy and capability gate. Agent cannot self-authorize.
4. Show chosen skill(s), reason, version, costs/context impact, actual invocations and override.
5. Auto-discover skill candidates from GitHub only when user triggers it or opted into update check; do not execute raw GitHub content.
6. Distinguish skill documents, installable packages, MCP servers, and standalone agent frameworks. They have different installation lifecycles.

## Windows/Linux implementation
Use application-owned versioned folders under `Skills/<id>/<version>` with a manifest and lockfile. GitHub fetch: remote allowlist, verified ref/commit hash, bounded download, safe extraction, staged validation, atomic directory swap, retained prior version, update state. Git CLI invocation uses path-safe argument lists; use PowerShell vs Bash only for user instructions, not unquoted shell concatenation. No global install by default. Disable postinstall scripts absent explicit, narrowly scoped consent and sandbox.

## Marketplace UI quality
Category rail + search/sort, polished content cards, filters with count, skill detail drawer, version compare modal, install progress with bytes, installed manager, updates queue, permission center, errors and rollback dashboard. All data labels truthful. If no rating infrastructure exists, do not invent ratings/reviews. Avoid copying Steam visual assets; borrow content/library organization principles only.

## Source provenance
Odysseus is strictly **ideas-only**. Do not copy code, components, assets or dependencies from that project. Other upstream references must undergo license, file-level ownership, security, compatibility and maintenance review before code reuse. No one-click 'install 250 skills'.

## Acceptance paths
- Install/enable/use/update/rollback/remove on both OSes; atomic failure recovery; dependency conflict; revoked permission; malicious archive; changed Git ref; user cancels mid-download; provider mismatch; disabled skill not invoked; skill tries unauthorized tool; catalog offline; source deleted; marketplace search accessible by keyboard; per-project enable persists; uninstall preserves shared dependencies.
