# IDE-grade desktop architecture decision addendum

## New user-provided reference
Screenshot proposes Electron, React/TypeScript, Tailwind, shadcn/ui/Radix, Zustand, Node.js, selective Rust, SQLite, Monaco, xterm.js+PTY, ACP/provider adapters, MCP, electron-builder. This is an architecture candidate, not proof that these components are already in Server Rock AI.

## Revised decision: architecture is a gated choice, not a predetermined rewrite
The prior masterplan treats Uno/.NET as fixed. The user has now supplied a credible Electron/React IDE architecture for evaluation. Preserve Uno as the baseline until an evidence-based comparison demonstrates the value of switching. Do not start a framework migration without explicit approval.

## Mandatory architecture spike
1. Inventory current Uno UI composition, services, native integrations, test coverage, runtime dependencies and actual bottlenecks.
2. Prototype one equivalent end-to-end workflow in existing Uno: chat -> agent plan -> terminal -> diff editor -> approve -> tests -> history.
3. Prototype the same workflow in an isolated Electron/React/TypeScript shell, without changing the shipping app. Compare a Tauri variant only if justified.
4. Measure keyboard behavior, accessibility, navigation, startup, idle memory, resource use during agent runs, packaging, Linux/Windows support, maintainability, dependency surface and development friction.
5. Estimate migration risk in terms of code that can be reused (C# core), rewritten (XAML), or adapted (native integration). Do not treat visual attractiveness alone as evidence for a framework switch.
6. Record a decision memo with reproducible demos and tests. Ask the user to approve any migration.

## Technology decisions independent of framework
- SQLite: assess transactional chat history, search indexing, operation journal and marketplace metadata; keep user-readable exports in Chat History and single-root policy. Use schema migrations, backups, WAL and corruption recovery.
- Monaco: evaluate editor/diff viewer embedding in Uno versus native editor controls. Electron/React has straightforward integration; Uno may require WebView bridging and accessibility/security work.
- Terminal: use a real OS PTY backend with permissioned commands, process lifecycle, cancellation, resize, scrollback and secret redaction. xterm.js is a frontend renderer, not the PTY backend.
- ACP: evaluate compatibility with supported coding agents, version negotiation, transport security and adapter boundaries. ACP is not a replacement for MCP; ACP is for agent-client interoperability, MCP for tools/resources.
- MCP: native lifecycle, permission scoping, process supervision, remote auth, logs, update/removal, untrusted tool-output handling.
- State: typed immutable workflow state and observable UI projections. Zustand only if adopting React; avoid introducing Node solely to copy a frontend architecture.
- UI: shared tokens, accessible primitives, command palette, activity center, keyboard shortcuts, responsive layout and navigation invariants. Tailwind/shadcn are React ecosystem tools, not drop-in Uno packages.
- Native: keep secure filesystem/process operations in a narrowly scoped backend with explicit IPC contracts and no renderer direct access to secrets or unrestricted shell.
- Packaging: signed, reproducible Windows/Linux installers, update/rollback, portable mode, single-root discoverability; Electron builder only if Electron selected.

## Decision matrix (to fill with measured evidence)
| Criterion | Uno/.NET current | Electron/React candidate | Tauri candidate |
|---|---|---|---|
| Existing code preservation | High | UI rewrite | UI rewrite |
| Monaco/xterm integration | Requires evaluation | Strong web ecosystem fit | Strong web ecosystem fit |
| Native C# services | Direct | IPC/sidecar | IPC/sidecar |
| Linux/Windows packaged behavior | Test | Test | Test |
| Memory and startup | Measure | Measure | Measure |
| UI development velocity | Measure | Measure | Measure |
| Installer/update complexity | Measure | Measure | Measure |
| Security boundaries | Review | Review | Review |

## Recommendation pending evidence
Keep C# domain services and core agent engine as stable contracts. Implement the missing IDE-grade workflows first. Treat Electron/React as a serious candidate, not a mandate; do not assume Uno is faster or better simply because it is the current stack.

## Agent-specific instruction
Do not silently override the user's prior Uno decision or this new architecture question. Flag the conflict in the decision register and present the comparative evidence to the user before any framework migration. No duration estimates or deadlines.
