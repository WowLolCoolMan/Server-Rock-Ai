# Model-aware automatic skill switching

Eligibility (deterministic): installed AND enabled AND OS-compatible AND provider/model-allowed AND capabilities present AND dependencies healthy AND permissions granted. User pin cannot override an incompatibility or denied permission.

Rank eligible skills by task relevance, explicit triggers, project language/framework, user pins, context budget and tool availability. Load lightweight summaries first; load detailed SKILL.md only when selected. Keep model prompts compact for 3B/7B/14B; decompose complex tasks for 30B; larger frontier models may execute broader workflows. Do not promise equal performance. Test and label model compatibility with observed evidence rather than guessing.

Tags: Universal (portable instructions only, with caveats), Claude-only, GPT-only, Gemini-only, Qwen-tested, local-LLM-tested, tool-calling-required, MCP-required, Windows/Linux, runtime-required. Allow multiple provider tags and explicit model version ranges. On active model change or tool disconnect, re-evaluate all active skills, revoke now-ineligible capabilities and notify user. Expose activation trace: candidate, excluded reason, selected reason, required permission, execution outcome.

Example: 'Debug Minecraft mod' on local Qwen 30B selects installed+enabled Java, loader/version-specific Minecraft, systematic debugging and engineering-quality skills; excludes Claude-only. It must verify compiler and loader versions, run tests and cite actual logs. No skill may silently claim a test passed.

Proposed pipeline: user task → project/context detection → installed/enabled registry → deterministic compatibility and permission gate → relevance ranking → progressive prompt/tool loading → verified execution → active-skills display → trace/history. LLM can propose a skill, but runtime enforces gate.
