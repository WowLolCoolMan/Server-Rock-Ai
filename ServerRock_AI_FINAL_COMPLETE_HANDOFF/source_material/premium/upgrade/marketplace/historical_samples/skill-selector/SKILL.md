---
name: skill-selector
description: >-
  Find the smallest compatible set of installed Agent Skills for the current task, using task intent, OS, runtime tools, model capabilities and user permissions; avoid false activation.
license: CC0-1.0
compatibility: Requires an application skill registry; tool operations still require host permissions.
---

# Skill Selector

1. Identify user intent and mandatory constraints.
2. Retrieve up to 10 candidates by skill name, description, use-when, tags and project context. Do not load every SKILL.md.
3. Check each candidate’s supported platforms, tools, script dependencies, license/review status, required data and active model's context/tool capabilities.
4. Exclude incompatible, untrusted, unapproved or irrelevant candidates with a short reason.
5. Rerank remaining candidates by relevance and prior verified outcomes. Select zero to three skills, keeping within context budget.
6. Load selected SKILL.md first; load references only on demand. Respect source skills' when-not-to-use clauses.
7. Permit explicit user override for selection, but never for missing host permission or safety checks.
8. Record IDs, source SHA, why selected/rejected, and validation outcome for later evaluation. Redact private data.

## Negative triggers

Don't select a skill because its folder name vaguely matches one token; don't use “thinking-out-loud” for every long structured prompt. Never claim an instruction-only model executed a tool.

## Test prompts

Positive: “Can you check whether these NuGet references match the installed Unity version?”
Negative: “Hi.”
