# Recovered feature backlog and decisions

## Core agent runtime
Typed inspect/plan/tool/edit/build/test/recover loop; bounded goals; activity journal; resumable execution; scoped approvals; branch/worktree isolation; parallel agents; cancellation; context budget and file inclusion inspector; diff-first review; test evidence.

## Provider and local-first AI
Ollama, OpenAI-compatible endpoints, LM Studio, local inference and cloud APIs; auto/local/cloud session profiles; explicit fallback consent; model catalog/picker; hardware-aware suggestions for ~24 GB VRAM machines; cost/health/rate-limit/capability routing; no silent remote prompt leakage.

## Marketplace and ecosystem
250–500 catalog discovery target; classify skills vs tools vs frameworks vs MCP servers; install/enable separation; GitHub update flags; permission and compatibility checks; skill creation/research suggestions; source pinning; clean uninstall; conflict/dependency management; provenance and audit log.

## UI and experience
Native polished design; Home, Chat, Projects, Agents, Models, Skills, Integrations, Activity, Settings; model transparency; folder map; chat history browser; robust navigation/overlay state; keyboard shortcuts/command palette; onboarding; approval inbox; agent execution replay; offline and failure UX; accessibility.

## Files, security and portability
One app-owned root, Chat History folder, encrypted credential vault or OS references, readable logs, sanitized diagnostic export, schema migrations, backups, cross-platform installers, signed updates, rollback, uninstall ownership, local-first privacy.

## Integrations
GitHub, MCP, ComfyUI workflows, Hugging Face model discovery, optional managed runtimes. Prefer API adapters and explicit dependency installation rather than bundling entire upstream applications.

## Deferred
Agent Machines/VMs; broad autonomy without approval; wholesale architecture rewrite; unverified donor source copying.

## Quality and release
Cross-platform unit/integration/UI smoke, CI, package installation, upgrade, rollback, corruption and disk-full tests, security and accessibility review, reproducible provenance and verified release evidence.
