# Observability, logs, errors and recovery UX

Use structured logging with correlation IDs and operation IDs, timestamps, component, outcome, error category, model/provider identity (never secret), workspace pseudonym where needed, redacted arguments. Separate human Activity timeline from debug logs and user-facing notifications. Log retention, export and deletion are user-controllable. No usage telemetry by default.

Crash breadcrumbs: active route, task/run ID, last durable event seq, build version, nonsecret OS and runtime details. On launch, inspect incomplete operations and show a 'Recovered session' state with what is known/unknown; do not blindly repeat side-effecting calls. Process supervisor owns cancellation and child cleanup independent of UI dialog lifetime.

Diagnose network/auth/local GPU/service/permission/path/git/installer failures with exact remediation. System status should show local model host health, keyring, storage available and update channel; avoid fake green status from configured-but-unreachable endpoints. Bug export previews included files/fields, redacts secrets and never uploads without deliberate approval.

Performance: instrument startup-to-interactive, navigation latency, event throughput, memory at idle, streaming frame smoothness, model time-to-first-token, terminal output throughput, search latency, disk IO and worktree operations. Establish targets after profiling on low-spec Win/Linux machines. If idle shell becomes heavy, profile before adding animations or extra overlays.
