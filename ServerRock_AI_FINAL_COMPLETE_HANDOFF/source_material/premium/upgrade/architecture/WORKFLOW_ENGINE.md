# Workflow engine: typed DAG, versioning and safe retries

Nodes: `agent_prompt`, `tool_call`, `condition`, `approval`, `test`, `parallel_group`, `join`, `artifact`, `notification` with versioned typed parameters; opaque secret refs, never inline secrets. Edges carry typed data, not unvalidated free-form shell interpolation. Validate graph connectivity, cycles (disallow except explicit bounded retry node), required inputs, incompatible ports, tool permission and provider capability at save and preflight.

Run pins workflow version and active profile; later edits create new version. Step states: pending, queued, executing, waiting approval, complete, failed, skipped, cancelled. Runner uses deterministic step IDs, idempotency keys for side effects and durable checkpoints. Partial success retains artifacts; retry only safe nodes. Parallel branches have resource limits and explicit join policy. UI reveals why a condition chose a branch.

Templates (seed, not fake executed flows): Code Audit & Repair, Implement Feature, UI Polish and Regression, Research & Compare, Build/Test/Release, Skill Vetting, Dependency Doctor, Project Onboarding, Multi-Agent Branch Implementation, Security Review (authorized targets only).

Builder experience: searchable palette; node detail inspector; drag/drop plus keyboard-ordered list editing; snap/grid; zoom/minimap; validation before run; dry-run of graph without tools; visible approvals; reusable subflows and exports. Never promise scheduled triggers until scheduler, persistence, OS behavior and user consent exist.
