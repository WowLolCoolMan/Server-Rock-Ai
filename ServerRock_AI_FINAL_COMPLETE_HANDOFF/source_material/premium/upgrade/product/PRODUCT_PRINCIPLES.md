# Premium product principles and measurable bar

## Identity
Server Rock AI is a complete, independent, local-first desktop AI work environment. It is not merely a chat window and not a visual clone. Reference Codex for supervised coding tasks, Claude for reading/writing quality, Discord for information architecture and persistent contexts, Steam for rich discoverable libraries and versioned installations. Do not copy protected product UI pixel-for-pixel, assets, names or interaction code.

## Product pillars
1. **One continuous context.** Project, selected model, agent task, permissions, open files, diff and terminal share one workspace identity. No repeated selection across screens.
2. **Progressive density.** New user sees simple chat + project entry; expert can expand sidebars, inspectors, multiple tabs, model stats, agent lanes, debugging. Hide complexity without hiding important state.
3. **Fast navigation.** Keyboard, command palette, back/forward, stable sidebars and contextual actions; every user action has a discoverable path.
4. **Genuine capability.** A disabled action explains prerequisites; it is never a clickable decorative stub. Prefer fewer complete features over dozens of inert pages.
5. **Trustworthy state.** Distinguish proposed/approved/applied/tested/committed, local/cloud, installed/enabled/selected, active/paused/stopped. Never imply invisible work.
6. **Owner control.** Network egress, privilege changes, downloads, installs, file writes, Git push and key access have explicit scope and revocation.
7. **Quality of details.** Crisp typography, sizing, spatial hierarchy, subtle motion, memorable empty states, readable code, accurate focus, rich toasts, native-feeling OS integration.
8. **Recoverable by design.** Crash, missing model, network outage and conflicting files produce a clear path back, not a blank pane.
9. **Portability.** Windows and Linux have comparable workflows and truthful documented differences.
10. **Evidence.** Every implemented capability has exercised tests and reproducible manual acceptance.

## Every feature definition-of-done
UI action + backend contract + state loading + explicit empty + error + offline + cancel/retry + accessible keyboard/focus + persistence when applicable + logs/provenance + unit/integration/UI tests + cross-platform checks + user docs + rollback. See `upgrade/quality/QUALITY_GATES.md`.

## Experience targets (design goals, not achieved claims)
- Typical project/session navigation should feel instantaneous without reloading heavy providers; avoid blocking main UI on model/marketplace calls.
- Streaming conversations update incrementally without shifting reading position unexpectedly.
- Keyboard-only primary tasks require no mouse; all overlays close with Escape and restore focus.
- Desktop resizing never traps actionable controls offscreen. Respect OS text scaling and reduced motion.
- Loading states preserve geometry and display purpose, not meaningless spinners.
- Use actual profiling to set startup/render/power thresholds; never publish invented benchmarks.
