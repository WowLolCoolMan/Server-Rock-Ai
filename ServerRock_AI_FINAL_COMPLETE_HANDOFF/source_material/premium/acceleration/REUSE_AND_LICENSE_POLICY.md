# Repository acceleration research — 2026-11-10

This donor map is **not** an instruction to copy arbitrary code. Reuse only after verifying exact revision, license, attribution, transitive dependencies, security and architecture compatibility.

## Strongest recommendations for the current Uno/.NET app
1. `modelcontextprotocol/csharp-sdk`: evaluate for native MCP integration, avoiding writing protocol infrastructure from scratch.
2. `CommunityToolkit/dotnet`: evaluate MVVM tooling to break up MainPage without a framework rewrite.
3. `unoplatform/uno.toolkit.ui`: evaluate cross-platform UI components rather than recreating controls.
4. `serilog/serilog` and `App-vNext/Polly`: evaluate structured logs and bounded retries.
5. `ollama/ollama`, `ggml-org/llama.cpp`, `comfyanonymous/ComfyUI`: integrate through supported APIs; do not transplant large runtimes into Core.
6. `odysseus-dev/odysseus`, `janhq/jan`, `Mintplex-Labs/anything-llm`, `open-webui/open-webui`, `danny-avila/LibreChat`: **feature and UX research**, not wholesale source migration.

## License red lines
- Odysseus README identifies **AGPL-3.0-or-later**. The user previously required **ideas only**, not incorporating Odysseus source. Maintain that constraint.
- An open-source label does not mean unrestricted reuse. GPL/AGPL obligations, trademark and attribution matter; even permissive code requires notice compliance.
- Do not copy proprietary official ChatGPT/Codex/Claude desktop code or assets. `lencx/ChatGPT` is an independently maintained open-source wrapper, not the official OpenAI app.
- A web/React/Python app cannot be transplanted directly into Uno XAML/C#. Reimplement ideas natively, or use a narrow API/sidecar with clear lifecycle if justified.
- Before direct reuse, verify LICENSE file, exact commit, copyright notices, subcomponent licenses, build compatibility, maintenance/security posture and test results. Flag unknown licenses as `RESEARCH_ONLY`.
- Do not auto-clone all repositories or run their setup scripts on the user's computer.

## Evidence and status
Web results verified current Odysseus and lencx/ChatGPT READMEs, Open WebUI desktop README and a Jan comparison. The other repository entries are **candidate pointers**, not audited codebases. Do not claim their current license, activity or exact API surface has been independently verified.
