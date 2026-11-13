# Gates that prevent half-baked releases

**Q0 Evidence:** exact repo HEAD/status, current build, existing implementation and true gaps. Historic plan is not proof.
**Q1 User need:** a job-to-be-done described as user journey with at least one positive, negative and recoverability scenario.
**Q2 Architecture:** correct native/Core boundary; versioned typed contract; tests; cancellation; no hidden platform assumptions.
**Q3 UI:** states for loading/empty/error/partial/disconnected; correct focus, overlays, popovers, responsive layout, keyboard/assistive tech and visual consistency.
**Q4 Backend:** persistence, permission enforcement, source validation, idempotency, retries, no orphan processes or duplicate effects.
**Q5 Integration:** end-to-end actual workflow with real IO where required; no mock-only pass.
**Q6 Cross-platform:** actual native build+run on Windows/Linux targets; OS-specific differences documented, no false distro claims.
**Q7 Reliability:** interruption, offline, corrupt data, auth expiry, low disk, stale file, timeouts, cancellation and rollback tests.
**Q8 Supply chain:** licenses, pinned deps, secrets redaction, signed verified downloads/releases as appropriate.
**Q9 Release:** clean installation, update, user data retention, uninstall, rollback, crash/telemetry choices, documentation and troubleshooting.

Feature status values: `not_started`, `in_progress`, `implemented_unverified`, `verified_windows`, `verified_linux`, `verified_both`, `blocked`, `deferred`. Do not substitute completion based on a green icon. Release-ready only when critical gates have collected reproducible evidence with artifact hash/build SHA.

For visual quality, reviewers inspect with a reference checklist: shell cohesion, alignment, density, legibility, interaction rhythm, hover/focus, empty/loading/error copy, resizable layout, layering, preserving scroll/cursor, command palette, art/icons, accessibility and smooth cancellation. The application must be pleasant for daily use, not simply functional.
