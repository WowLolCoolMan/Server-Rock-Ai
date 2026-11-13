# Development acceleration: aggressive but testable

Goal: **reduce avoidable duplicated engineering work**, not guarantee a calendar halving. Optimize critical path, not raw agent-generated line count.

## Initial baseline and preparation
- Inventory current branch and working tree; run baseline tests and record environment.
- Build a requirement-to-file matrix: present / partial / absent / uncertain. Stop reimplementing existing features.
- Rank the 130 original tasks by dependencies and impact; group them into 12 vertical slices, not 130 disconnected commits.
- Select at most 4 high-leverage dependency candidates after license/security review (MVVM toolkit, MCP SDK, UI toolkit, diagnostics).
- Establish CI gates: fast unit tests per change; integration and packaged smoke per milestone.

## 12 recommended vertical slices
1. Baseline/acceptance harness and feature inventory.
2. Typed operation state machine + event journal + approvals.
3. Service boundaries, DI, MVVM pilot on one screen.
4. Native design tokens and navigation with snapshot testing.
5. Chat reliability and data storage preservation.
6. Provider capabilities, route disclosure, first-run model wizard.
7. Single-folder storage contract, migrations and diagnostics.
8. Marketplace manifest, staging, install ledger and rollback.
9. Marketplace UI, compatibility routing and auto-selection.
10. MCP lifecycle, permissions and agent activity.
11. Full cross-platform end-to-end testing, performance and fault injection.
12. Signed installers, upgrade/uninstall, beta feedback and release evidence.

## Parallel execution rules
- Agent A: Core state machine, persistence, tests.
- Agent B: Uno UI, design system, accessibility (only after shared contracts defined).
- Agent C: Marketplace manifests, installer and test fixtures (independent folder).
- Agent D: QA harness, documentation and evidence ledger.
- Each agent uses a separate git worktree/branch. Merge only after passing checks. No two agents edit the same file concurrently.
- Define shared interfaces and acceptance tests before parallel implementation; avoid integration churn.
- Use focused, evidence-based research spikes per dependency candidate and abandon low-value ports.

## Avoid false speed
Do not skip security, cancellation, migration or packaged validation. Do not run bulk destructive refactors. Do not replace working native functionality with a web stack. A faster code generation run is not a faster reliable release.

## Milestone evidence template
- Change summary and exact files
- Before/after user-visible behavior
- Tests run (commands, environment, pass/fail)
- Screenshots/accessibility checks when applicable
- Data migration impact and rollback
- Dependencies added with license/version
- Known limitations and next unblocked task
