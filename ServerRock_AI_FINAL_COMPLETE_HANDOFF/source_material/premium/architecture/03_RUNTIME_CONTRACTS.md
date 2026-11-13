# Typed contracts that all interfaces must share

## Operations
`Operation {id,kind,workspaceId,actorId,createdAt,updatedAt,state,scope,policy,progress,correlationId,result,error}`. States: drafted, preflight, awaitingApproval, queued, running, verifying, completed, failed, cancelled, recovering. Every transition validated and persisted. Events have monotonically increasing sequence IDs, schema version and replay support.

## Capability descriptors
`ModelCapabilities`: provider, model ID, local/cloud, context, tools, vision, structured output, streaming, availability, cost/resources when known, provenance of capabilities. `SkillManifest`: source commit, version, compatibility constraints, dependencies, permissions, hash and license. `ToolCapability`: filesystem/network/process/credential scope, supported platform, user consent. No model is trusted to self-declare privileged tool access.

## IPC/API
Versioned request/response and event schemas, cancellation tokens, backpressure, event replay after reconnect, authorization at the gateway, safe serialization, explicit timeouts, clear error codes. Never serialize secrets to UI event streams. For .NET sidecar: supervise process, authenticate local channel, restrict listening scope and avoid unauthenticated localhost TCP. Compare named pipes / Unix domain sockets with gRPC as appropriate.

## Data lifecycle
Structured SQLite database for chat metadata, operations, skills and indexing. Conversation content and attachments may be stored under one discoverable root, with readable Markdown/JSON exports in **Chat History**. Atomic writes, schema migrations, ownership ledger, retention and recovery. Credential storage encrypted or OS-backed with documented external paths.
