# Server Rock AI — consolidated development specification
This package preserves the original 50-skill catalog, 30 GitHub research references and 15 MCP candidates, and adds the decisions from the conversation through November 10, 2026.

**Scope:** implementation guidance, NOT source-code changes or proof of current repository functionality. GitHub links are research leads and must be verified before shipping. The 50 entries are capability proposals, not 50 guaranteed plug-and-play skills.

## Decisions
- Target architecture: prototype Tauri 2 + Rust using existing Electron web UI; freeze but preserve early Uno implementation. Retire old shells only after Windows/Linux feature parity.
- Skill Marketplace: large curated opt-in catalog; install, enable, pin, update, uninstall independently. Skills are not all loaded at once.
- Skill router: task-aware and model-aware; select only installed, enabled, compatible, relevant skills.
- Model support: portable SKILL.md format where possible; provider-specific skills tagged and gated.
- GitHub updates: track upstream source/path/commit, check periodically, stage and verify, support rollback.
- All optional components: tracked clean uninstall including owned dependencies and caches; never delete user data silently.
- Chat History: visible user-selectable 'Chat History' folder, search, readable exports, migration, backups.
- Production-Grade Engineering: optional anti-generic AI engineering and design skills, requiring idiomatic implementation and real tests.

## Suggested order
1. Inventory current Electron/Uno features and user data; establish parity checklist.
2. Define versioned manifests, capabilities, permissions, skill lifecycle and shared contracts.
3. Prototype Tauri shell + local/cloud model bridge + persistence.
4. Implement installation ownership ledger, staged updates, rollback and clean removal.
5. Build marketplace and deterministic router.
6. Implement chat history migration and accessible folder.
7. Add curated skills, MCP integrations, QA and Windows/Linux release gates.

## Acceptance gate
Every feature must have a real end-to-end test and a visible error/recovery path. Never claim a skill works with every model without testing it.
