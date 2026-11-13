# Server Rock Agent Host

This is the first migration seam for the Electron desktop prototype. It is a
small, local-only .NET process that speaks versioned JSON lines over stdin/stdout.

The host currently exposes `runtime.health`. It intentionally does not duplicate
chat or model logic yet; the next slice will register application services around
the existing `ServerRock.Core` provider and persistence contracts.

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
