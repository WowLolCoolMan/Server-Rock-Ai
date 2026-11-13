# Reliability and recovery contract

Each operation has a stable ID, owner, input summary, scope, state, timestamps, cancellability, external effects and result. Suggested states: drafted, preflighting, awaiting_approval, queued, running, verifying, completed, failed, cancelled, recovering.

Persist state transitions in a journal. Recover interrupted work on restart with idempotency checks; never blindly replay a destructive action. Distinguish safe retry from duplicate side effects.

Failure UX: describe what failed, whether anything changed, where logs live, next safe action, and rollback availability. Support cancellation through network, download, execution and UI layers.

Tests: kill process during file write, disk full, permission denied, invalid JSON, partial model download, provider timeout, malformed MCP response, duplicate install, interrupted update, corrupted manifest, removable drive missing and version rollback.
