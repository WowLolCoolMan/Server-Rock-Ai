# QA and release gate matrix — never confuse a mocked specification with tested reality

## Testing pyramid

- Domain unit tests for conversation persistence, router error classification, model capabilities, approval scoping, run state machine, diff/undo, file bounds, skill registry, Git adapters and update manifest verification.
- Integration tests with fake provider/agent/MCP transports, real filesystem temp directories and Git fixtures, cancellation/timeouts, restart recovery and OS service stubs.
- Native UI automation and actual-window inspection on Windows and Linux: keyboard-only, screen readers, scaling, all overlay/focus paths, navigation, loading/error/empty/offline, accessibility of modal controls.
- Artifact tests: publish clean builds, launch exact installed binary, upgraded binaries, rollback, user-data retention, path handling, native libraries, keyring failure, token leakage checks, signature and checksum validation.

## High-priority scenario catalogue

| ID | Scenario | Must prove |
|---|---|---|
| T-01 | First launch with no model runtime | Actionable connect path, no phantom connected status |
| T-02 | Chat streaming then route switch | Session keeps streaming; draft and scroll survive |
| T-03 | Provider rate limit mid-run | Honest partial output, classified error, approved fallback or explicit block |
| T-04 | Sticky local with cloud configured | No content sent outside loopback without override consent |
| T-05 | Two agent tasks modifying same repo | Isolated worktrees or explicit safe exclusion; no silent overwrite |
| T-06 | Window closed during active run | Durable reconciled status; no fabricated success or repeated side effects |
| T-07 | Late tool result after cancellation | Discard/attach to cancelled run appropriately, never update unrelated run |
| T-08 | File changed by external editor | Conflict view; no data loss; backup and user choice |
| T-09 | Untrusted skill with executable install step | Quarantined/staged; informed permission and license review |
| T-10 | Marketplace remote unavailable | Update status unknown/stale; installed prior copy remains intact |
| T-11 | Settings modal with drawer/pet overlay | No click-through, correct z-order, focus restoration |
| T-12 | Resize app at 560px while dropdown open | Popup stays within viewport; all essential controls accessible |
| T-13 | Linux keyring disabled | Clear block/recovery without writing secrets to settings JSON |
| T-14 | Git push interrupted after remote accepted | Reconcile remote state; never blindly push a second time |
| T-15 | Upgrade with legacy conversations | No branch/attachment loss; reversible migration and content hashes |
| T-16 | Installer signing absent | Release fails rather than publishing unsigned build as official |
| T-17 | Keyboard-only skill install/update | All controls reachable, permission details announced, Escape semantics |
| T-18 | MCP server hangs | Scoped timeout, user cancellation, task error without UI freeze |
| T-19 | GPU memory pressure | Scheduler queues/refuses with truthful reason; no OOM retry loop |
| T-20 | Reconnect to an interrupted task | Monotonic event replay, no duplicate approvals or stale status |

See `index/PREMIUM_ACCEPTANCE_70.json` for the original 70 structured test scenarios; `source_material/independent/ui/07-PAGE_HIERARCHY_AND_LAYER_REGRESSIONS.md` for 30 additional precise native UI regression exercises. Execute both, reconcile duplicates and record target SHA and platform.

## Evidence language

- `Source observed`: file/line and commit, not proof of execution.
- `Unit test passed`: include test command, exact suite and actual output; not cross-platform validation.
- `Live UI checked`: date/OS/resolution and screenshots with controlled test data; not a packaging check.
- `Packaged smoke passed`: exact binary artifact/hash and launch record; not full install or accessibility acceptance.
- `Release verified`: signed package, clean install, runtime, upgrade/uninstall, rollback, privacy/security and accessibility evidence on a stated supported platform.
- `Not tested` / `Unknown` are valid states. Do not estimate or invent them away.

## Release checklist

Native build and core tests; static UI audit; actual window smoke; sampled key workflows; accessibility and 560px narrow UI; update manifest and hash; signing/provenance; privilege and credentials; first-run and offline; log redaction; release artifact integrity; installed upgrade, rollback, uninstall retention; Windows 11 and stated Linux distro(s). For all unverified OSes, describe exact limitations. Update `docs/NATIVE_RELEASE_ACCEPTANCE.md` with observed evidence only.
