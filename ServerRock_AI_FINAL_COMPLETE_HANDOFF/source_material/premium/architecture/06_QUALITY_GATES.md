# Acceptance, threat model and cross-platform gates

## Foundation gates
- No renderer directly invokes OS commands or accesses raw credentials.
- Operation journal survives crash/restart and reconciles incomplete side effects.
- Cancel, retry, permission denial and offline states tested for all major workflows.
- Skill and MCP install/update/remove pass malicious archive, symlink escape and dependency-conflict tests.
- Model routing reveals actual provider and required data transfer consent.
- Single app-owned folder remains discoverable; external system dependencies clearly listed.
- PTY process trees are terminated or deliberately retained with explicit ownership; no orphaned hidden tasks.
- SQLite migrations preserve chat histories, branches, settings and install ledgers.
- Windows and Linux packaged smoke tests include UI, accessibility, model routing, terminal, editor, skill lifecycle and recovery.
- Diagnostics have correlation IDs and scrub credentials and sensitive payloads.

## Electron-specific (if selected)
Sandbox renderer, context isolation, strict preload bridge, no Node integration in renderer, navigation restrictions, CSP, signed/verifiable updates, controlled child processes, secure local IPC, app packaging smoke.

## Evidence
Record exact command, environment, output, test results and artifact paths. No claim of superiority over Codex/Claude without matched user studies and reproducible benchmarks. No claims of successful tests that were not run.
