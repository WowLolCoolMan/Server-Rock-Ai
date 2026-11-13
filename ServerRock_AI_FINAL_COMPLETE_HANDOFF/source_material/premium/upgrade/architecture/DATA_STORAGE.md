# Data layout, persistence, migrations, backup and recovery

## Respect prior single-root owner requirement
One discoverable **app-owned data root**, configurable when platform permits, with folders: `Profiles/`, `Chat History/` (readable exports), `Sessions/`, `Projects/` (metadata only), `Agent Runs/`, `Models/` (app-owned downloads only), `Skills/`, `MCP/`, `Workflows/`, `Logs/`, `Cache/`, `Backups/`, `Updates/`, `Diagnostics/`. External user projects and installed third-party runtimes remain where owner selected; OS keyrings are outside root and should not be copied into portable backups.

Current repo uses platform data paths and JSON stores: keep functional and readable. A proposed SQLite database is an implementation option for indexed history/runs, **not license to delete JSON**. Use an incremental migration with backup, transactional copy, hash count checks, dual-read compatibility where needed and explicit rollback. Canonical record must be clear; avoid divergent JSON and DB writes.

## Suggested relational entities
`workspaces`, `projects`, `conversations`, `messages`, `message_branches`, `attachments`, `tasks`, `agent_runs`, `operation_events`, `artifacts`, `approvals`, `tool_invocations`, `models`, `providers`, `skills`, `skill_installs`, `skill_grants`, `mcp_connections`, `workflows`, `workflow_versions`, `workflow_runs`, `downloads`, `notifications`, `settings`, `migrations`, `backups`. Many records may stay JSON until transition justified.

IDs are UUIDs, timestamps UTC ISO8601, schemaVersion explicitly stored, source/provenance tracked, foreign keys and cascade semantics reviewed. Search index should exclude temporary content and secrets. Events append-only with redaction; snapshots can be rebuilt from event journal. Use OS credential store with only opaque key references in DB; never keep API keys in Settings JSON or backup exports.

## Import/export
Human-readable Markdown/JSON conversation exports, with attachments only when opted in. Show preview count, proposed duplicates, conflict policy and privacy warning. Import cancel writes nothing. Repeat import idempotent; edited imported chats not overwritten. Include schema version and archive checksum; reject archive path traversal/zip bombs and invalid role metadata.

## Backup & restore
Backup creates versioned manifest + checksums; consistent snapshot of data with writes quiesced/transactional; preserve unrecognized fields for future versions. Restore previews diffs, data paths and effects; backup existing data and require confirmation. Interrupted migration/restore leaves previous profile intact; automated tests verify. Uninstall never deletes user data without explicit separate consent.

## Retention
Local-first and no telemetry by default; user controls temp chat, logs, exported diagnostics, model cache cleanup and history deletion. System disk-pressure warning before huge downloads. Display actual disk usage (measured vs estimated), owner of each directory and clean removal plan.

## Tests
Concurrent write/crash/truncation; power loss simulation; permission denied; disk full; locked DB; symlink/root traversal; invalid schema; duplicate import; partially applied migration; foreign key integrity; backup corruption; non-ASCII paths; high-volume 10k chats/events; Linux secret service absent.
