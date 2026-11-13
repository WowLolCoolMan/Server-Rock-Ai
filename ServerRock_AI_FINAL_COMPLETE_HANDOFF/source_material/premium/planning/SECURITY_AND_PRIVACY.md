# Security and privacy threat model

## Assets
API credentials, chat contents, project source, local model files, user filesystem, execution permissions, remote endpoints, marketplace supply chain.

## Threats and controls
- Prompt injection in GitHub skills, retrieved documents, MCP tool results: treat as untrusted data, never as higher-priority instructions.
- Skill supply chain: pinned commits, verified source, manifest hash, dependency review, staging install, permission diff, rollback and quarantine.
- Remote routing: explicit consent and display provider/endpoint; no silent cloud fallback.
- Secrets: encrypted portable vault or OS credential store; never plaintext in settings, logs, telemetry, crash reports or exported bundles.
- Filesystem: restrict writes to declared roots, prevent traversal and symlink escape, request approval for external project edits.
- Execution: typed tool permissions, scope limits, confirmation for destructive actions, audit journal, cancellation and safe rollback where possible.
- Network: endpoint allowlisting for managed integrations, TLS validation, per-provider consent, avoid leaking prompt text in URLs.
- Diagnostics: opt-in export, redaction tests, retention controls and user inspection before sharing.
- Multi-user: ownership/permissions and secure file access; no shared mutable vault by default.
- Updates: signed releases where supported, verified checksums, atomic replacement and rollback.

## Security test cases
Malicious SKILL.md requesting secrets; fake upstream README install command; zip-slip package; symlinked skill path; duplicate provider alias; credential string in exception; malicious tool output; update rollback; offline startup; revoked MCP permission.
