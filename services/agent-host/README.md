# Skai Desktop Agent Host

This is the first migration seam for the Electron desktop prototype. It is a
small, local-only .NET process that speaks versioned JSON lines over stdin/stdout.

The host exposes `runtime.health`, `models.list`, `projects.list`,
`tasks.list`, `scheduled.list`, `conversations.list`, `conversation.save`, and
`chat.complete`, using the existing `ServerRock.Core` provider and persistence
contracts. The Electron renderer receives only the narrow context-isolated
bridge; it never receives Node.js or direct filesystem access.

For disposable protocol tests, `SRAI_CONVERSATIONS_PATH` can point the
conversation routes at an isolated JSON file. It is optional; normal desktop
launches use the Core application-data path.

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
