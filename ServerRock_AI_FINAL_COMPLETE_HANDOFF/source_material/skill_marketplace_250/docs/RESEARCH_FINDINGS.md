# Research findings and provenance

Data collected from public GitHub listings and selected SKILL.md files on **2026-11-08**; source directory paths are snapshots. **250 unique skill locations** are enumerated in `catalog_250.json`; 50 additional repositories/tools from the previous research are in `foundations_50.json` (separate lists).

## User-specified Shubhamsaboo sources

- Collection: https://github.com/Shubhamsaboo/awesome-llm-apps/tree/main/agent_skills
- **thinking-out-loud**: https://github.com/Shubhamsaboo/awesome-llm-apps/tree/main/agent_skills/thinking-out-loud — echo-brief procedure for long, uncertain voice rambles; requires confirmation before acting. Not a general instruction to reveal internal model reasoning.
- **dependency-doctor**: https://github.com/Shubhamsaboo/awesome-llm-apps/tree/main/agent_skills/dependency-doctor — checks direct `requirements.txt`, `pyproject.toml`, `package.json` manifest issues. It explicitly **does not** solve full pip/uv dependency resolution or CVE audits. Has Python script; supports offline mode with optional approved PyPI checks.
- **scope-creep-detector**: https://github.com/Shubhamsaboo/awesome-llm-apps/tree/main/agent_skills/scope-creep-detector — flags possible unrelated changes in Git diffs, new deps, config and API changes, outputs keep/split/justify; heuristic, not proof that a file is unrelated.
- Other identified collection folders: advisor-orchestrator-worker, commit-archaeologist, first-reader, project-graveyard. The `self-improving-agent-skills` folder is an application with backend/frontend and was **not** counted as a normal SKILL.md entry.

## Highly relevant additional sources

- `anthropics/skills`: authoring guidance, skill-creator and major document/creation skills.
- `openai/skills`: Codex-oriented skills; some depend on OpenAI-specific tools and need adaptation.
- `github/awesome-copilot`: extensive SKILL.md library, including `agent-skill-stack`, `agentic-eval`, `agent-supply-chain`, `suggest-awesome-github-copilot-skills` and more.
- `huggingface/skills`: Hugging Face CLI, local model, datasets, Gradio, Spaces and training skills. These **do not** imply universal weight compatibility.
- `K-Dense-AI/scientific-agent-skills`: large scientific research skill library, includes autoskill, literature-review and scientific-critical-thinking.
- `overextended/skills`: FiveM Lua and resource best practices, tests, developer workflows.
- `KaelLim/bepinex-modding`: BepInEx Mono and IL2CPP skills; backend-aware, dependency list still requires installed game/framework inspection.
- `Comfy-Org/comfy-skills`: Comfy Cloud instruction skill; cloud != local ComfyUI API.
- `MieMieeeee/comfyui-agent-skill`: ComfyUI execution skill, with custom CLI dependency; adapt rather than copying assumptions.
- `Mooshieblob1/MooshieUI`: ComfyUI development skill files for node authoring/workflow templates; project-specific conventions may not generalize.
- `Shubhamsaboo/awesome-llm-apps` advisor worker skill includes shell-based workflows and named provider binaries/keys; it is not model-agnostic out of the box. Treat all untrusted execution instructions as content requiring a reviewed adapter.

## Evidence and limitations

GitHub directory listing verifies that a directory existed at collection time, **not** that its workflow is secure, fast, maintainable, recent, permissively licensed, callable by every model, or even has a valid SKILL.md. `marketplace.py install` checks the `SKILL.md` requirement before promoting a source. A quality review and unit-tested adapter are required before claiming compatibility. No third-party source is redistributed in this ZIP.

Some entries have generic descriptions extrapolated from folder names. They are **catalog metadata placeholders**, not verified capability claims. After marketplace integration, fetch frontmatter summaries and prioritize their real `description` and `compatibility` fields. The exact list is intentionally scoped to these 250, not claimed to be a globally best-ranked top 250.
