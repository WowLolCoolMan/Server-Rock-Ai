# Skai Desktop Agent Host

This is the first migration seam for the Electron desktop prototype. It is a
small, local-only .NET process that speaks versioned JSON lines over stdin/stdout.

The host exposes `runtime.health`, `models.list`, `projects.list`, and
`chat.complete`, using the existing `ServerRock.Core` provider and persistence
contracts. The Electron renderer receives only the narrow context-isolated
bridge; it never receives Node.js or direct filesystem access.

Run it manually:

```powershell
dotnet run --project services/agent-host/ServerRock.AgentHost.csproj -- --stdio
```

Then send one JSON request per line:

```json
{"v":1,"id":"check-1","method":"runtime.health"}
```

The process is launched by the Electron main process, not the renderer. The
renderer receives only the narrow `serverRockAgent.request` bridge exposed by the
preload script.
