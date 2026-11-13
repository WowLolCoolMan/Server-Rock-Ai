# Providers, cloud/local choices, fallback and capability discovery

## Separate concepts
**Provider:** endpoint/auth protocol (OpenAI-compatible, Anthropic, future adapters). **Model:** immutable provider-specific ID + endpoint identity, modality/context/tool capability and availability. **Agent:** tool-using harness (native tool-loop, Codex App Server, ACP-compatible agent/official CLI) that may use one or more models. Do not treat text inference as a full coding agent. **Profile:** policy (Local, Cloud, Auto) + permissions, privacy, budget, priority.

## Routing rules
1. Derive model identity and capabilities from trusted provider registry / probe; never from model-generated claims.
2. Explicit user selection wins over automatic routing unless unavailable; explain failure and offer alternatives, do not silently override.
3. Local profile **never sends prompts/files to a remote inference endpoint**; validate endpoint trust/location, not a text label.
4. Cloud profile uses selected cloud provider; log visible request context and costs only if verified.
5. Auto resolves availability, capability needs, privacy policy, resource occupancy and task constraints; show selected route/reason before side effects. Persistent consent to cloud egress must be explicit and revocable.
6. Fallback eligible only for classified recoverable failure (e.g., rate limit/quota/capacity). Authentication failure requires reauth; content safety failure must not be bypassed; model OOM offers smaller local model/resource adjustments.
7. Fallback cannot assume same tool fidelity, context length, vision or output format. Reassess tool capability and reformat/reduce context; ask consent if provider/privacy scope changes. Avoid duplicating prior side effects by checkpoint.
8. 'Sticky local' honored until user changes profile. No invisible cloud calls for skill selection, embeddings, search or telemetry under Local profile.

## Discovery/health
Probe known local hosts only with bounded loopback requests and timeout, never broad network scans. Register each model as `(provider, endpointHost, modelId)`; same names on different hosts are distinct. Store last seen, capabilities status verified/estimated/unknown, measured latency/sample count if applicable. Installation/download from Hugging Face/Ollama uses source/license/storage/disk checks and explicit user clicks.

## Hardware-aware scheduling
Measure actual available memory only via supported platform APIs; estimate model footprint separately and label it. Bound concurrent local model reservations; prefer queue to OOM; indicate service startup, model loading, warm and in-flight phases. Gracefully recover from runtime crash without corrupting conversation.

## Agent adapters
- Native tool-loop over OpenAI-compatible model: robust structured tool calls, JSON validation, tool approval, bounded retry.
- Codex integration: implement only from supported/verified App Server protocol; do not scrape desktop sessions or bypass auth.
- ACP integrations: protocol version/capability negotiation, streaming, cancellation, working-directory scope, tool calls, credential isolation.
- Claude Code bridge: only official supported interfaces, not invented CLI flags; test Windows/Linux auth, missing executable and cancellation.
- MCP: tool protocol, not model inference or full agent. Must have separate transport, trust and permission boundaries.

## Tests
Cloud outage with local fallback permission; local-only profile with untrusted remote URL; insufficient context/tool capabilities; two models same name; rate limit mid-stream; auth failed; unsupported tool; cancellation at route switch; unsafe retry after file write; GPU OOM queue; invisible network attempts forbidden.
