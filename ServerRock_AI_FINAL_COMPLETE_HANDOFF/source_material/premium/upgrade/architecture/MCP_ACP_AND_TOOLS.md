# Extensibility: MCP, ACP, tools and agent providers

## Four noninterchangeable layers
Provider adapter = inference transport and model capabilities. Agent adapter = autonomous coding harness/context/tool loop. MCP = tools/resources/prompts exposed through server transport. Skill = declarative instruction/workflow and optional dependencies. **Never collapse all four into a generic 'plugin' abstraction without metadata**.

## MCP connection lifecycle
Candidate → Inspect metadata/manifest → Configure transport (stdio/HTTP subject to supported version) → Permission review → Start supervised server → Tool schema validation and health check → Enable per workspace → Invoke with scoped grants → Observe logs → Disable/upgrade/remove. Every connection shows source, transport, executable, working dir, network domains, environment requirements (secret refs), tools, auth and last health. Use timeouts, cancellation, process tree cleanup and bounded outputs. Support protocol version negotiation. No unreviewed dynamic tool-schema escalation.

## ACP/coding-agent adapters
Discover official supported protocol or CLI. Check local installation, protocol version, login state, tool capability, cancellation, file access, streaming semantics and reconnection support. Preserve native job events in Server Rock's journal. Distinguish external harness session from Server Rock run ID; map them with stable correlation fields. Do not import browser cookies/credentials or spoof official clients.

## Tool executor contract
Registry entry: toolId, version, JSON input/output schema, required grants, side effects, idempotency class, platform support, timeout, observable status, redaction. Validate arguments both before approval and before execution. Separate read-only tools and write-capable ones. Unsupported tool produces explicit structured result. Output truncation stores safe reference to full bounded log, not silent removal.

## Marketplace compatibility
A vendor-named skill may still be model-agnostic if tool requirements are generic. Eligibility requires installed + enabled + policy allowed + compatible OS + agent/tool/model capabilities + dependencies. Version pinning, selected skill and explanation recorded per invocation. Unknown capabilities are not assumed.
