# Definition of done and release acceptance

Feature is done only if it has working user flow, clear error/empty/loading states, test evidence, accessibility verification, documentation, no credential leakage and a rollback or recovery story when stateful.

Release candidate gates:
- Clean install and upgrade on Windows and Linux.
- All app-owned paths discoverable; no surprise persistent writes.
- Conversation and settings migrations preserve existing data.
- User can inspect and manage local/cloud routing and permissions.
- Skill install/update/remove and MCP lifecycle work under failures.
- No dead controls, trapped overlays or hidden background operations.
- Diagnostics exports are sanitized and actionable.
- Packaging checksums/signing and rollback behavior verified.
- No critical or high-severity unresolved security issues without explicit documented release decision.
- User-facing release notes and known limitations match actual evidence.

The release gate is evidence-based, never tied to a calendar date.
