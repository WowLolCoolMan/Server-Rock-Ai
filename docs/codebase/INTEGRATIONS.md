# External Integrations

## Core Sections (Required)

### 1) Integration Inventory

| System | Type (API/DB/Queue/etc) | Purpose | Auth model | Criticality | Evidence |
|--------|---------------------------|---------|------------|-------------|----------|
| OpenAI-compatible endpoints | HTTP API | Chat/model discovery and streaming | Optional session/provider key; explicit remote consent | high | `native/SkaiDesktop.Core/Services/OpenAiCompatibleProvider.cs` |
| Ollama/LM Studio and local runtimes | Local HTTP APIs | Local model discovery, pull/load and chat | Loopback/local route validation | high | `native/SkaiDesktop.Core/Services/ModelLibraryService.cs` |
| DuckDuckGo | HTTP search | Opt-in bounded web snippets/citations | Public endpoint; no account credential | medium | `native/SkaiDesktop.Core/Services/WebSearchProvider.cs` |
| GitHub | Git + REST/device flow | Repository browse, clone, review, commit/push | OS-protected credential/device flow | high | `native/SkaiDesktop.Core/Services/GitHubApiClient.cs`, `GitHubWorkspaceService.cs` |
| ComfyUI | Local HTTP API | Image workflow queue/poll | Local endpoint validation | medium | `native/SkaiDesktop.Core/Services/ComfyImageProvider.cs` |
| MCP stdio/Streamable HTTP | Tool protocol | Explicitly granted tool calls | Scoped connection/permission records | high | `native/SkaiDesktop.Core/Services/Mcp*`, `native/SkaiDesktop.Core/Infrastructure/` |

### 2) Data Stores

| Store | Role | Access layer | Key risk | Evidence |
|-------|------|--------------|----------|----------|
| Application-data JSON files | Settings, chats, projects, queue, memory, operations | Core stores and atomic writer | Corruption/migration; bounded readers mitigate | `native/SkaiDesktop.Core/Infrastructure/` |
| User-selected project filesystem | Context, reviewed edits, terminal workdir | Workspace path/editor/runner services | Path traversal, secrets, symlinks; guards exist | `native/SkaiDesktop.Core/Infrastructure/Workspace*` |
| OS credential store | GitHub/provider credentials where supported | GitHub credential service | Platform parity `[TODO]` | `native/SkaiDesktop.Core/Services/GitHubCredentialStore.cs` |

### 3) Secrets and Credentials Handling

- Credentials are not intended for ordinary settings JSON; GitHub credentials are protected and provider keys can remain session-only.
- Repository hygiene and redaction gates are present; a full Linux keyring parity run is `[TODO]`.
- Rotation/expiry is implemented for GitHub flows; generalized provider-key rotation is `[TODO]`.

### 4) Reliability and Failure Behavior

- HTTP calls use bounded content, cancellation and classified failures; local model fallback requires explicit selection/consent.
- Timeout/cancellation behavior is implemented in provider, workflow, terminal and MCP paths; exact per-integration limits are source-defined.
- Durable operation/workflow journals reconcile interrupted work without blindly replaying side effects.

### 5) Observability for Integrations

- Local structured app logging and activity/status history exist; provider attempts and workflow events can be searched/exported.
- Metrics/tracing/APM are not present: `[TODO]`.

### 6) Evidence

- `native/SkaiDesktop.Core/Services/OpenAiCompatibleProvider.cs`
- `native/SkaiDesktop.Core/Services/GitHubApiClient.cs`
- `native/SkaiDesktop.Core/Services/McpStreamableHttpClient.cs`
- `native/SkaiDesktop.Core/Infrastructure/AtomicFileWriter.cs`
- `docs/CAPABILITY_MATRIX.md`
