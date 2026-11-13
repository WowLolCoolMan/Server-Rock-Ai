# Universal Skill Intelligence: Selection, Creation, Discovery, and Evaluation

## Goal

Give users a competent assistant for almost any ordinary task through **task understanding -> retrieval -> skill execution -> evidence-based verification**. Do **not** imply any model can do every task: a model without tools cannot interact with local files; an unsupported image model cannot run a ComfyUI workflow; no skill grants new permissions by itself.

## 1. Selection without flooding the context window

Maintain two indices: short lexical BM25-like metadata index and optional embedding index. Ingest `name`, `description`, use-when, tags, required tools, supported OS, dependencies, input/output types, benchmark history, and source provenance. Retrieval is separate from model prompt generation.

**Proposed selection:**

```
Task request + active workspace + selected model capabilities
  -> classify intent (code, research, image, data, documents, etc.)
  -> retrieve top 10 candidate skills by lexical/embedding retrieval
  -> eliminate missing required runtime/tools/permissions
  -> rerank by relevance, compatibility, confidence and verified past outcome
  -> select 0 to 3 skills within token budget
  -> load selected SKILL.md into task-scoped context
  -> load references or scripts on demand; user approves privileged tools
  -> execute -> verify output -> save redacted evidence/feedback
```

Do not auto-activate skills when none matches. Do not force “thinking-out-loud” on every direct question; follow its use-when contract. For explicit skill selections by user, prefer manual activation subject to safety/tool compatibility. For local Qwen/small models use deterministic intent filters plus short skill descriptors and a plan/verification wrapper. For cloud models do not assume provider-specific tools exist without adapter support.

**Sample score (tune empirically, not an established benchmark):** `0.35 semantic relevance + 0.25 intent match + 0.20 verified historical task success + 0.10 tool readiness + 0.10 user preference`; hard-block permission denial/incompatible runtime. Collect labeled positive/negative examples. Evaluate top-1 and top-3 precision/recall, false-trigger rate, token cost and time-to-completion.

## 2. New skill creator

Ask (or infer safely) objective, trigger phrases, when-not-to-use, input artifacts, required tools, reference docs, output/checks, platform constraints and permissions. Generate a **local draft** with `SKILL.md`, optional `references/`, `scripts/`, `assets/`, tests. Validate frontmatter and no implicit tool access. Test with positive/negative triggering prompts and representative supported models, show results, require user approval before enabling or publishing. Use reference skills such as `anthropics/skills/skills/skill-creator`, `openai/skills/skills/.system/skill-creator`, and `Shubhamsaboo/awesome-llm-apps/agent_skills/*` for patterns; license remains important.

A user should be able to say, “We keep debugging C# NuGet failures—make a .NET dependency skill.” The app discovers current SDK and package versions, drafts a project-specific skill, runs diagnostics against an opted-in fixture project, and proposes review. It must not secretly install packages or modify arbitrary repositories.

## 3. Skill scout and research addition

Triggered when a user says “find a skill for ...”, asks for new capabilities, or a task repeatedly fails due to missing domain knowledge. Search GitHub APIs, README and SKILL.md candidates and relevant code (public by default), score trust/quality/usefulness, compare existing skills. Return suggestions **without downloading/executing**; user opens details and approves install. Set a 30-day stale check if user opts in; otherwise manual only. Respect rate limits and avoid scanning the user’s projects without explicit request.

Sources worth indexing: `github/awesome-copilot`, `VoltAgent/awesome-agent-skills`, `anthropics/skills`, `openai/skills`, `huggingface/skills`, `K-Dense-AI/scientific-agent-skills`, `overextended/skills`, `coreyhaines31/marketingskills`, and the 50 foundations list.

## 4. “Universal Problem Solver” meta skill

If request is clear and small, answer directly with zero skills. Otherwise: set outcome, constraints and verification criteria; inspect relevant capabilities; identify knowledge gaps; suggest an existing skill or create a draft; break into small verified tasks; execute with user-approved tools; report actual test evidence, limitations and next steps. Only use extra models where a cost/time/risk threshold justifies them. This workflow does not override content, privacy, or OS safeguards.

## 5. Provider strategy

| Model runtime | Instruction skill | Tool skill | Image/ComfyUI | Hugging Face model |
|---|---|---|---|---|
| Local Ollama / llama.cpp / LM Studio | Works with adequate context | Requires tested function/tool adapter | ComfyUI handled through a separate service | GGUF/compatible formats only through supported host |
| Cloud OpenAI-compatible | Works | Needs provider-specific schema/stream adapter and user permissions | External service if separately connected | Only if hosted endpoint supports that task |
| ComfyUI | Not a general chat agent | Workflow actions via local ComfyUI API | Native task backend, nodes/models verified | Specific ComfyUI loaders/nodes only |
| Hugging Face Hub | Repository/catalog, not a universal runtime | Depends on actual runtime | Diffusers/ComfyUI/backend-dependent | Task/weights/license/VRAM/runtime compatibility required |

## 6. Skill evaluation

For every skill, create a set of 10+ success scenarios, 5+ negative triggers, at least one permission rejection, one missing-dependency test and one canceled execution. Record baseline vs skill-enabled: accuracy, successful actions, manual intervention, elapsed time, context tokens, cost and unexpected changes. Report measurements per model/endpoint and skill SHA; a skill is not globally “elite” on the strength of popularity.
