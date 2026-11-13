# SERVER ROCK AI — CONSOLIDATED IMPLEMENTATION SPECIFICATION
Revision: 2026-11-10 | Purpose: Codex on Windows and Claude on Linux

**Start here.** This is a consolidated, evidence-labeled engineering reference, NOT proof of implementation or authorization for destructive operations. Read `00_AGENT_READ_FIRST.md`, `MASTER_INDEX.md`, `audit/REQUIREMENTS_AND_CONFLICT_RESOLUTION.md`, and `audit/IMPLEMENTATION_READINESS.md` before changing code.

## Mandate
Build a premium, reliable, extensible AI desktop environment with clear agent workflows, provider routing, model and skill control, MCP integrations, readable user data, and complete Windows/Linux support. Preserve working code and user data. Keep Agent Machines in backlog. Do not incorporate Odysseus source.

## Critical architecture status
The existing Uno/.NET app is the baseline. Electron+React+.NET and Tauri+React+.NET are candidates, not authorized migrations. Run equivalent IDE workflow prototypes and seek an explicit architecture decision before replacing the UI.

## Contents
- `roadmap/`: 100 core steps, 20 competitive steps, 10 storage steps.
- `architecture/`: IDE foundation, typed runtime contracts, UI framework decision, 30 architecture tasks.
- `planning/`: detailed product, UX, data, security, test, release and collaboration contracts.
- `history/`: previous conversation requirements and conflict analysis.
- `research/`: original skill/MCP research; see catalog completeness audit.
- `acceleration/`: donor candidate list, license triage and reuse guidelines.
- `legacy_references/`: prior ZIP documents retained as historical reference, NOT extra authority.
- `audit/`: consolidated requirements, data models, acceptance tests, traceability and remaining gaps.

## Execution rule
Inspect current repository HEAD, working tree and build before claiming any item exists or is broken. Build small reversible slices with test evidence and cross-platform review. A specification is not an implementation. Never silently migrate frameworks, credentials, user data or local model files.

## No timeline
This pack deliberately contains no project deadlines or estimates as product requirements.
