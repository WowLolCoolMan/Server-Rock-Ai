# Server Rock AI — Premium Desktop Handoff

Start: [`START_HERE_PREMIUM.md`](START_HERE_PREMIUM.md) → [`AGENT_HANDOFF_PROMPT.md`](AGENT_HANDOFF_PROMPT.md).

**Purpose:** specification/reference bundle that preserves the prior consolidated 119-file plan and adds concrete production-grade native Windows/Linux UI, navigation, agent, model, marketplace, persistence, permissions, accessibility, tests and release contracts. It is **not** a compiled app, code fix, verified skill installer, or authorization to rewrite the repo.

Current source baseline: https://github.com/WowLolCoolMan/Server-Rock-Ai @ `4ad39cfb89ddf97cd1ddd4533c4b879d8124ac95` (2026-11-07), Uno Platform / C# / XAML / .NET 10. Preserve current native code; framework changes need explicit owner approval.

New docs: `upgrade/ux/`, `upgrade/architecture/`, `upgrade/marketplace/`, `upgrade/platform/`, `upgrade/agents/`, `upgrade/product/`, `upgrade/quality/`. Machines can read `upgrade/data/*.json` and `upgrade/schemas/*.json`; scripts under `tools/` perform offline package checks.

Old documents under `planning/`, `history/`, `architecture/`, `roadmap/`, `audit/`, `research/`, `legacy_references/`, etc. remain included. Odysseus = ideas ONLY. No time estimates. No silent local→cloud fallback or secret storage in plaintext.
