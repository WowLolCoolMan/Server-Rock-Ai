# Deterministic, model-aware skill routing
## Selection pipeline
1. Read user request and current task context.
2. Enumerate ONLY installed AND enabled skills (plus manually pinned compatible skills).
3. Enforce provider/model allow/deny tags and capability requirements (tool calls, MCP, filesystem, context, runtimes, OS).
4. Reject missing dependencies or unapproved permissions before model selection.
5. Rank eligible skills by semantic task relevance, explicit triggers, project context, user pins and estimated token cost.
6. Load only top relevant skill summaries; progressively load detailed SKILL.md instructions when needed.
7. Expose Active Skills panel showing why selected, missing requirements, and changes.
8. Recalculate on model switch, tool disconnect, task phase change, install/disable or permission revocation. Never bypass hard compatibility gates.

## Tags
Universal (only if genuinely portable); Claude-only; GPT-only; Gemini-only; Qwen-tested; Local-LLM-tested; tool-calling-required; MCP-required; Windows/Linux; requires-external-runtime. A provider-specific skill remains visible but disabled for incompatible models. User pin cannot override an incompatibility or permission gate.

## Capability adaptations
Frontier model: full workflow where supported. 30B coder: smaller batches, focused context, same verification. 7B–14B: more app-managed decomposition. Small models: narrow tasks with explicit tests and limits. No promise of equivalent results; show tested compatibility levels and known limitations.

## Example
User: 'Debug my Minecraft mod'; active Qwen 30B coder. Candidate skills: Minecraft modding, Java, systematic debugging, production-grade engineering. Claude-only skills excluded. Compile against exact Minecraft/mod-loader versions, execute applicable GameTests, inspect actual logs. On model change, re-evaluate eligibility without reinstall.
