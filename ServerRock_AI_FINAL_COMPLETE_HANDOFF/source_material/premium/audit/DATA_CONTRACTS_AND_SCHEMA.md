# Suggested typed domain contracts (design targets; adapt to existing code)

- `Operation`: id, kind, actor, workspace, scope, state, progress, created/updated, correlationId, idempotencyKey, cancellation and recovery metadata.
- `OperationEvent`: operationId, monotonic sequence, schemaVersion, timestamp, eventKind, redacted payload, status.
- `ModelDescriptor`: provider, modelId, capabilities (tool calling/vision/context/structured output), runtime constraints, health, local/cloud, verification status.
- `ProviderRoute`: selected endpoint, fallback policy, permitted data classes, approval scope, reason, traceId.
- `SkillManifest`: stableId, publisher, repo URL, pinned commit, version, license, compatible models/capabilities, OS, tools, dependencies, permissions, content digest, activation entrypoint.
- `SkillInstallation`: manifest digest, installed revision, enabled flag, ownership manifest, update candidate, rollback revision, install state.
- `McpServer`: transport, command or URL, exposed capabilities, credentials reference, permissions, health, ownership, lifecycle state.
- `Workspace`: root, project metadata, agent worktree associations, context inclusion rules, external location indicator.
- `Conversation`: stable id, folder, title, timestamps, branches, messages, attachments, provider/model per message, export status.
- `CredentialReference`: provider, alias, secure-store identifier, creation/update timestamp, never raw token in logs or config.
- `DiagnosticBundle`: user-approved content inventory, redaction summary, support ID, export path.
- `InstalledComponent`: type, source, files owned, shared dependency refs, version, update state, uninstall policy.

Storage: SQLite with WAL for indexed structured state when appropriate; use migration/version checks and backups. Chat History must have user-readable exports, not require direct SQLite manipulation. Never make SQLite and export files two competing authorities without defining canonical source and synchronization.
