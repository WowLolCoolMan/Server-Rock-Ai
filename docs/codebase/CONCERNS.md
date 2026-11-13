# Codebase Concerns

## Core Sections (Required)

### 1) Top Risks (Prioritized)

| Severity | Concern | Evidence | Impact | Suggested action |
|----------|---------|----------|--------|------------------|
| high | Linux build/runtime and packaged acceptance are not freshly verified in this checkout | `docs/CAPABILITY_MATRIX.md`, `docs/NATIVE_RELEASE_ACCEPTANCE.md` | Cross-platform claims cannot be release-complete | Install/enable .NET 10 on supported Linux runner and run exact artifact scenarios |
| high | Signed installer/feed and clean upgrade/uninstall evidence remain open | `packaging/native/`, `docs/NATIVE_RELEASE_ACCEPTANCE.md` | Release cannot be claimed production-ready | Run signing and lifecycle gates with real artifacts |
| high | Native Popup accessibility focus still reports the underlying composer in prior live evidence | `docs/CAPABILITY_MATRIX.md`, `docs/GOAL_EXECUTION.md` | Keyboard/screen-reader actions may target the wrong surface | Reproduce with supported native accessibility harness and fix coordinator/bridge |
| medium | Electron/agent-host migration is not feature parity complete | `README.md`, `docs/ELECTRON_MIGRATION_STAGE1.md` | Two surfaces can diverge in behavior | Keep migration incremental and label unsupported routes |

### 2) Technical Debt

| Debt item | Why it exists | Where | Risk if ignored | Suggested fix |
|-----------|---------------|-------|-----------------|---------------|
| Large, high-churn native partial surface | Incremental extraction preserved behavior | `native/ServerRock.Native/MainPage.*.cs` | Event/focus regressions | Extract typed coordinators with live acceptance per seam |
| No configured formatter/coverage threshold | Existing gates emphasize compiler and behavioral checks | Codebase scan output, `package.json` | Style/coverage drift is less visible | `[ASK USER]` choose formatter and minimum coverage policy |
| Capability register still has source-level `requires_live_repo_recheck` statuses | Handoff is a seed, not proof | `ServerRock_AI_FINAL_COMPLETE_HANDOFF/index/COMBINED_REQUIREMENTS_126.csv` | Requirements may be over- or under-claimed | Maintain live row-level status ledger with evidence links |

### 3) Security Concerns

| Risk | OWASP category (if applicable) | Evidence | Current mitigation | Gap |
|------|--------------------------------|----------|--------------------|-----|
| Approved project commands/files are high-impact side effects | A03 | `native/ServerRock.Native/MainPage.CodingAgent.Proposals.cs`, `WorkspaceCommandRunner.cs` | Review, path bounds, cancellation and undo | Full OS sandbox/isolation is `[TODO]` |
| Remote prompt/data disclosure | A exfiltration/privacy | `MainPage.Chat.cs`, `LocalEndpoint.cs` | Explicit consent and local/remote labels | Complete negative scenario suite and Linux parity `[TODO]` |
| MCP/tool permissions | A01/A05 | `McpToolPermissionGate.cs`, `McpConnectionRegistry.cs` | Explicit grants, revocation and schema checks | Broader third-party server fixture coverage `[TODO]` |

### 4) Performance and Scaling Concerns

| Concern | Evidence | Current symptom | Scaling risk | Suggested improvement |
|---------|----------|-----------------|-------------|-----------------------|
| In-memory native conversation/UI state | `MainPage.Chat.cs`, `ConversationStore.cs` | Bounded provider context but rich UI state remains in process | Large histories can increase render/persistence cost | Add measured scenario benchmarks and incremental rendering |
| Generated pet/image assets increase checkout/package size | `assets/pets/`, packaging project | Large binary assets are tracked | Slower clone/build/package operations | Optimize only with visual regression evidence |
| Full-text attachment mode is intentionally more expensive | `ChatAttachmentPrompt.cs`, `AppSettings.cs` | Explicit mode can send more bounded context | Larger prompts and provider latency | Keep retrieval default and expose size/status honestly |

### 5) Fragile/High-Churn Areas

| Area | Why fragile | Churn signal | Safe change strategy |
|------|-------------|--------------|----------------------|
| `docs/GOAL_EXECUTION.md` | Canonical evidence log touched by every slice | Scan lists 209 recent commits | Append only; never rewrite evidence |
| `scripts/audit-native-ui.cjs` | Contracts mirror many visible controls | Scan lists 116 changes | Add one contract with each UI change and run it |
| `native/ServerRock.Native/MainPage.xaml` + partials | Shared state/event wiring | Scan lists 134/45 changes | Feature-local partials, serial builds, live focus checks |

### 6) `[ASK USER]` Questions

1. [ASK USER] Which Linux distribution and desktop environment should be the supported release target for the required native/package acceptance run?
2. [ASK USER] Should the future Electron/agent-host surface remain a migration prototype until native feature parity, or is there an owner-approved cutover threshold?
3. [ASK USER] What minimum test coverage percentage, if any, should become a release gate?

### 7) Evidence

- Codebase scan command output from the required acquisition workflow
- `docs/CAPABILITY_MATRIX.md`
- `docs/NATIVE_RELEASE_ACCEPTANCE.md`
- `README.md`
- `native/ServerRock.Native/MainPage.*.cs`
