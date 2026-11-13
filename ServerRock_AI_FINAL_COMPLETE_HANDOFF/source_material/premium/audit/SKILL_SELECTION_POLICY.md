# Model-aware skill routing contract

Eligibility gate: installed AND enabled AND version compatible AND OS/runtime compatible AND model/tool capability compatible AND permissions granted AND dependency health verified. Never ask the model to decide its own identity or grant itself authority; obtain model identity/capabilities from the trusted provider registry. Unknown capabilities must be probed safely or treated as unsupported.

After deterministic filtering, relevance rank eligible skills by task intent, user selection, context budget and confidence. Load minimal manifest summaries first; load full SKILL.md only for selected skills. Show chosen skill, why, prerequisites, tool scope and overrides. Preserve manual opt-in/out. A Claude-only skill must never be auto-loaded for a non-Claude model. A generic Markdown skill may still require actual tool capabilities.

'Anti-generic-AI' quality skills must demand domain-specific design, idiomatic conventions, realistic tests, readable architecture, lint/static analysis and reproducible proof. They must not falsify authorship, remove attribution or conceal copied code.
