# Recovered Codex Hybrid Router work — separate project, potential design reference

User wanted Codex CLI/Desktop local/cloud fallback, automatic Ollama model discovery, `/model`/picker switching, per-session cloud/local/auto profiles, sticky local mode, smart routing, independent parallel cloud/local agents, bounded goal loop, MCP and clean uninstall.

Earlier assistants reportedly built an **independent MIT-licensed community Codex Hybrid Router** plugin + CLI companion:
- v0.2.0: Python router/installer/test ZIP, 25/25 unit tests and 2/2 black-box tests reported.
- v0.3.0: `plugin.json`, `mcp.json`, skills, bounded goal loop, 38/38 tests reported in isolated/simulated environments.
- Monitored `codex-router run --profile auto` used noninteractive `codex exec --json`; quota/rate-limit fallback retried once only if no tool activity occurred.
- No migration of interactive/Desktop sessions; Desktop profile changes require new thread/restart.
- Parallel execution used separate CLI sessions and Git worktrees, not native in-app concurrent agents.
- Uninstall preserved unrelated MCP servers, credentials, user edits, threads, repositories and Ollama models.

## Do not conflate
This separate plugin is not evidence Server Rock AI has these features. Treat it as a possible source of **patterns** or separately licensed components only after the actual artifact, provenance and compatibility are inspected. Do not assume the router is already checked into Server Rock AI.

## Integration candidates
Provider capability registry; safe fallback before side effects; per-session routing policy; model discovery; bounded goal loop; isolated worktrees; reversible plugin install/uninstall; explicit tool permissions and evidence-backed completion.
